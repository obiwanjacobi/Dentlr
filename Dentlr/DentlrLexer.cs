using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;

namespace Dentlr;

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
    /// Once set, explicitly or by detecting the first indent, it is used to validate subsequent indents.
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
            _tokenBuffer.Enqueue(token);

            var wsTokens = new List<IToken>();
            // next token after a newline could be an indent
            var tokenIndentLength = ScanWhitespaceTokens(ref token, wsTokens, out IToken? nonWsToken);

            if (tokenIndentLength > 0)
            {
                if (IndentSize == 0)
                    IndentSize = tokenIndentLength;

                if (tokenIndentLength % IndentSize > 0)
                    throw new InvalidIndentException($"Invalid indent length at {token.Line}:{token.Column}");

                var indentCount = tokenIndentLength / IndentSize;
                var indentTokens = ProcessIndentDedent(token, indentCount);
                EnqueueTokens(wsTokens, indentTokens, nonWsToken);
            }
            else
            {
                EnqueueTokens(wsTokens);

                if (nonWsToken is null || nonWsToken.Type != _eolTokenId)
                    FlushScheduledDedents();

                if (nonWsToken is not null)
                    _tokenBuffer.Enqueue(nonWsToken);
            }
        }
        else if (token is not null)
        {
            if (token.Type == DentlrLexer.Eof)
                FlushScheduledDedents();

            _tokenBuffer.Enqueue(token);
        }

        if (_tokenBuffer.Count > 0)
            return _tokenBuffer.Dequeue();

        // we ran out of tokens -> cleanup scheduled dedents
        FlushScheduledDedents();

        if (_tokenBuffer.Count > 0)
            return _tokenBuffer.Dequeue();

        // the end
        return null;
    }

    private List<IToken> ProcessIndentDedent(IToken eolToken, int indentCount)
    {
        var indentTokens = new List<IToken>();
        var scheduleCount = _dedentSchedule.Count;
        
        // text is less indented than before
        if (indentCount < scheduleCount)
        {
            // emit dedent tokens
            for (int i = 0; i < scheduleCount - indentCount; i++)
            {
                indentTokens.Add(_dedentSchedule.Pop());
            }
        }
        // text is more indented than before
        else if (indentCount > scheduleCount)
        {
            // emit indent tokens
            for (int i = scheduleCount; i < indentCount; i++)
            {
                var indentToken = CreateIndentToken(eolToken, indentCount);
                indentTokens.Add(indentToken);

                var dedentToken = CreateDedentToken(eolToken);
                _dedentSchedule.Push(dedentToken);
            }
        }

        return indentTokens;
    }

    private void EnqueueTokens(IList<IToken> wsTokens, IList<IToken> indentTokens, IToken? nonWsToken)
    {
        EnqueueTokens(wsTokens);
        EnqueueTokens(indentTokens);

        if (nonWsToken is not null)
            _tokenBuffer.Enqueue(nonWsToken);
    }

    private void EnqueueTokens(IList<IToken> tokens)
    {
        foreach (var token in tokens)
            _tokenBuffer.Enqueue(token);
    }

    private int ScanWhitespaceTokens(ref IToken eolToken, List<IToken> wsTokens, out IToken? nextToken)
    {
        var wsCache = new List<IToken>();
        var indentLength = 0;

        var token = base.NextToken();
        while (token is not null &&
            token.Type != Eof &&
            String.IsNullOrWhiteSpace(token.Text))
        {
            if (token.Type == _eolTokenId)
            {
                eolToken = token;
                wsTokens.AddRange(wsCache);
                wsTokens.Add(token);
                wsCache.Clear();
                indentLength = 0;
            }
            else
            {
                indentLength += token.Text.Length;
                wsCache.Add(token);
            }

            token = base.NextToken();
        }

        nextToken = token;
        if (token is not null &&
            indentLength == 0)
            indentLength = token.Column;
        
        return indentLength;
    }

    private void FlushScheduledDedents()
    {
        while (_dedentSchedule.Count > 0)
        {
            _tokenBuffer.Enqueue(_dedentSchedule.Pop());
        }
    }

    private IToken CreateIndentToken(IToken eolToken, int indent)
        => new DentlrToken(
            new string(' ', indent * IndentSize),
            _indentTokenId, eolToken.Line + 1, 0,
            eolToken.Channel, -1, 0, indent * IndentSize,
            eolToken.TokenSource, eolToken.InputStream);

    private IToken CreateDedentToken(IToken eolToken)
        => new DentlrToken(
            String.Empty,
            _dedentTokenId, eolToken.Line + 1, 0,   // line/col refer to indent counter part
            eolToken.Channel, -1, -1, -1,
            eolToken.TokenSource, eolToken.InputStream);

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