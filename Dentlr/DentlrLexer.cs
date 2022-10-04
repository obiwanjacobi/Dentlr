using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;

namespace Dentlr
{
    /// <summary>
    /// Base class for the Antlr Lexer that will generate INDENT and DEDENT tokens.
    /// </summary>
    /// <remarks>
    /// Specify `options { superClass=Dentlr.DentlrLexer; }` in your lexer grammar file.
    /// </remarks>
    public abstract class DentlrLexer : Lexer
    {
        private readonly Queue<IToken> _tokenBuffer = new();
        private readonly Stack<IToken> _dedentSchedule = new();

        private int _indentTokenId;
        private int _dedentTokenId;
        private int _eolTokenId;

        /// <summary>
        /// Pass through ctor.
        /// </summary>
        /// <param name="input">Must not be null.</param>
        protected DentlrLexer(ICharStream input)
            : base(input)
        { }

        /// <summary>
        /// Passs through ctor.
        /// </summary>
        /// <param name="input">Must not be null.</param>
        /// <param name="output">Must not be null.</param>
        /// <param name="errorOutput">Must not be null.</param>
        protected DentlrLexer(ICharStream input, TextWriter output, TextWriter errorOutput)
            : base(input, output, errorOutput)
        { }

        /// <summary>
        /// Gets or sets the number of spaces one indent represents.
        /// Once set, explicitly or detecting the first indent, it is used to validate subsequent indents.
        /// </summary>
        public int IndentSize { get; set; }

        /// <summary>
        /// Returns true when the INDENT, DEDENT and EOL tokens are initialized (greater than zero).
        /// </summary>
        public bool AreTokensInitialized
            => _dedentTokenId > 0 && _indentTokenId > 0 && _eolTokenId > 0;

        /// <summary>
        /// Initializes the token ids for the INDENT, DEDENT and EOL tokens.
        /// </summary>
        /// <param name="indentTokenId">Pass in the Id of the INDENT token.</param>
        /// <param name="dedentTokenId">Pass in the Id of the DEDENT token.</param>
        /// <param name="eolTokenId">Pass in the Id of the EOL token.</param>
        /// <remarks>
        /// Call InitializeTokens by overriding the NextToken method in your lexer class:
        /// <code>
        /// public override IToken? NextToken()
        /// {
        ///     if (!AreTokensInitialized)
        ///         InitializeTokens(INDENT, DEDENT, EOL);
        ///     return base.NextToken();
        /// }
        /// </code>
        /// Or call InitializeTokens when you create your lexer instance:
        /// <code>
        /// var stream = new AntlrInputStream(source);
        /// var lexer = new MyLexer(stream);
        /// lexer.InitializeTokens(MyLexer.INDENT, MyLexer.DEDENT, MyLexer.EOL);
        /// </code>
        /// </remarks>
        public void InitializeTokens(int indentTokenId, int dedentTokenId, int eolTokenId)
        {
            _indentTokenId = indentTokenId;
            _dedentTokenId = dedentTokenId;
            _eolTokenId = eolTokenId;
        }

        /// <summary>
        /// Injects INDENT and DEDENT tokens based on position analysis of the tokens following newline (EOL) tokens.
        /// </summary>
        /// <returns>Returns the new token or null.</returns>
        /// <exception cref="InvalidOperationException">Thrown when InitializeTokens() was not called.</exception>
        /// <exception cref="InvalidIndentException">Thrown when an invalid indent size was encountered.</exception>
        public override IToken? NextToken()
        {
            if (!AreTokensInitialized)
                throw new InvalidOperationException("The Dentlr tokens are not initialized. Call InitializeTokens().");

            if (_tokenBuffer.Count > 0)
                return _tokenBuffer.Dequeue();

            var token = base.NextToken();
            if (token?.Type == _eolTokenId)
            {
                var newlineToken = token;
                _tokenBuffer.Enqueue(newlineToken);

                // next token after a newline could be an indent
                token = base.NextToken();
                var tokenIndentLength = GetTokenIndentLength(token);

                if (token is not null &&
                    newlineToken.Line < token.Line &&
                    tokenIndentLength > 0)
                {
                    if (IndentSize == 0)
                        IndentSize = tokenIndentLength;

                    if (tokenIndentLength % IndentSize > 0)
                        throw new InvalidIndentException($"Invalid indent length at {token.Line}:{token.Column}");

                    var indentCount = tokenIndentLength / IndentSize;
                    var scheduleCount = _dedentSchedule.Count;

                    // text is less indented than before
                    if (indentCount < scheduleCount)
                    {
                        // emit dedent tokens
                        for (int i = 0; i < scheduleCount - indentCount; i++)
                        {
                            _tokenBuffer.Enqueue(_dedentSchedule.Pop());
                        }
                    }

                    // text is more indented than before
                    if (indentCount > scheduleCount)
                    {
                        // emit indent tokens
                        for (int i = scheduleCount; i < indentCount; i++)
                        {
                            var indentToken = CreateToken(_indentTokenId, i, token);
                            _tokenBuffer.Enqueue(indentToken);

                            var dedentToken = CreateToken(_dedentTokenId, i, token);
                            _dedentSchedule.Push(dedentToken);
                        }
                    }
                }
                else if (token is not null &&
                    tokenIndentLength == 0)
                {
                    while (_dedentSchedule.Count > 0)
                    {
                        _tokenBuffer.Enqueue(_dedentSchedule.Pop());
                    }
                }
            }

            if (token is not null)
                _tokenBuffer.Enqueue(token);

            if (_tokenBuffer.Count > 0)
                return _tokenBuffer.Dequeue();

            return null;

            static int GetTokenIndentLength(IToken token)
             => String.IsNullOrWhiteSpace(token.Text)
                ? token.Text.Length
                : token.Column;
        }

        private IToken CreateToken(int tokenId, int offset, IToken fromToken)
            => new DentlrToken(
                String.Empty,
                tokenId, fromToken.Line, fromToken.Column + offset * IndentSize,
                fromToken.Channel, fromToken.TokenIndex, fromToken.StartIndex, fromToken.StopIndex,
                fromToken.TokenSource, fromToken.InputStream);

        private sealed class DentlrToken : IToken
        {
            public DentlrToken(string text,
                int type, int line, int column, int channel,
                int tokenIndex, int startIndex, int stopIndex,
                ITokenSource tokenSource, ICharStream inputStream)
            {
                Text = text;
                Type = type;
                Line = line;
                Column = column;
                Channel = channel;
                TokenIndex = tokenIndex;
                StartIndex = startIndex;
                StopIndex = stopIndex;
                TokenSource = tokenSource;
                InputStream = inputStream;
            }

            public string Text { get; }
            public int Type { get; }
            public int Line { get; }
            public int Column { get; }
            public int Channel { get; }
            public int TokenIndex { get; }
            public int StartIndex { get; }
            public int StopIndex { get; }
            public ITokenSource TokenSource { get; }
            public ICharStream InputStream { get; }
        }
    }
}