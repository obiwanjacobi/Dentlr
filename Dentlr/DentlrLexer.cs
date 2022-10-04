using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;

namespace Dentlr
{
    public abstract class DentlrLexer : Lexer
    {
        private readonly Queue<IToken> _tokenBuffer = new();
        private readonly Stack<IToken> _dedentSchedule = new();

        private int _indentTokenId;
        private int _dedentTokenId;
        private int _eolTokenId;

        protected DentlrLexer(ICharStream input)
            : base(input)
        { }

        protected DentlrLexer(ICharStream input, TextWriter output, TextWriter errorOutput)
            : base(input, output, errorOutput)
        { }

        public int IndentSize { get; set; }

        public bool AreTokensInitialized
            => _dedentTokenId > 0 && _indentTokenId > 0 && _eolTokenId > 0;

        public void InitializeTokens(int indentTokenId, int dedentTokenId, int eolTokenId)
        {
            _indentTokenId = indentTokenId;
            _dedentTokenId = dedentTokenId;
            _eolTokenId = eolTokenId;
        }

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
                        IndentSize =tokenIndentLength;

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