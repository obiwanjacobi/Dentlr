using System;
using Antlr4.Runtime;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Dentlr.UnitTests;

public class LexerTokenTests
{
    private const string EOL = "\n";
    private const string INDENT = "  ";

    private readonly ITestOutputHelper _output;

    public LexerTokenTests(ITestOutputHelper output)
        => _output = output;

    [Fact]
    public void TestLevels_Gradual()
    {
        var source =
            "level0" + EOL +
            INDENT + "level1" + EOL +
            INDENT + INDENT + "level2" + EOL +
            INDENT + "level1" + EOL +
            "level0"
            ;

        var tokens = new[]
        {
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.INDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.INDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.DEDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.DEDENT,
            SpaceTokenLexer.WORD,
        };

        AssertTokens(source, tokens);
    }

    [Fact]
    public void TestLevels_JumpIndent()
    {
        var source =
            EOL +                       // INDENTs are only detected after EOLs
            INDENT + "level1" + EOL +   // to set indent size
            "level0" + EOL +
            INDENT + INDENT + "level2" + EOL +
            INDENT + "level1" + EOL +
            "level0"
            ;

        var tokens = new[]
        {
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.INDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.DEDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.INDENT,
            SpaceTokenLexer.INDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.DEDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.DEDENT,
            SpaceTokenLexer.WORD,
        };

        AssertTokens(source, tokens);
    }

    [Fact]
    public void TestLevels_JumpDedent()
    {
        var source =
            "level0" + EOL +
            INDENT + "level1" + EOL +
            INDENT + INDENT + "level2" + EOL +
            "level0"
            ;

        var tokens = new[]
        {
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.INDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.INDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.DEDENT,
            SpaceTokenLexer.DEDENT,
            SpaceTokenLexer.WORD,
        };

        AssertTokens(source, tokens);
    }

    [Fact]
    public void TestLevels_EndDedent()
    {
        var source =
            "level0" + EOL +
            INDENT + "level1"
            ;

        var tokens = new[]
        {
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.INDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.DEDENT
        };

        AssertTokens(source, tokens);
    }

    [Fact]
    public void TestLevels_NewlineEndDedent()
    {
        var source =
            "level0" + EOL +
            INDENT + "level1" + EOL
            ;

        var tokens = new[]
        {
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.INDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.DEDENT
        };

        AssertTokens(source, tokens);
    }

    [Fact]
    public void TestLevels_RepeatIndent()
    {
        var source =
            "level0" + EOL +
            INDENT + "level1a" + EOL +
            INDENT + "level1b" + EOL
            ;

        var tokens = new[]
        {
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.INDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.DEDENT
        };

        AssertTokens(source, tokens);
    }

    [Fact]
    public void TestLevels_ExtraNewLines()
    {
        var source =
            "level0" + EOL +
            INDENT + "level1" + EOL +
            EOL +
            EOL +
            "level0" + EOL +
            INDENT + "level1a" + EOL +
            EOL +
            INDENT + "level1b"
            ;

        var tokens = new[]
        {
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.INDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.DEDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.INDENT,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.EOL,
            SpaceTokenLexer.WORD,
            SpaceTokenLexer.DEDENT
        };

        AssertTokens(source, tokens);
    }

    [Fact]
    public void TestLevels_InvalidIndentException()
    {
        var source =
            EOL +
            INDENT + INDENT + "level2" + EOL +      // indent size is set here
            INDENT + "level1"                       // indent length is too short!
            ;

        Action errorAction = () => AssertSpaceTokens(source, []);
        errorAction.Should().Throw<InvalidIndentException>();

        errorAction = () => AssertIgnoreSpaceTokens(source, []);
        errorAction.Should().Throw<InvalidIndentException>();

        errorAction = () => AssertMultipleSpaceTokens(source, []);
        errorAction.Should().Throw<InvalidIndentException>();
    }

    private void AssertTokens(string source, int[] expectedTokens)
    {
        AssertSpaceTokens(source, expectedTokens);
        AssertIgnoreSpaceTokens(source, expectedTokens);
        AssertMultipleSpaceTokens(source, expectedTokens);
    }

    private void AssertSpaceTokens(string source, int[] expectedTokens)
    {
        var stream = new AntlrInputStream(source);
        var lexer = new SpaceTokenLexer(stream);
        lexer.InitializeTokens(SpaceTokenLexer.INDENT, SpaceTokenLexer.DEDENT, SpaceTokenLexer.EOL);
        var tokens = lexer.GetAllTokens();
        //_output.Write(lexer.TokenTypeMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key), tokens);
        Tokens.Assert(tokens, expectedTokens, nameof(SpaceTokenLexer));
    }

    private void AssertIgnoreSpaceTokens(string source, int[] expectedTokens)
    {
        var stream = new AntlrInputStream(source);
        var lexer = new IgnoreSpaceLexer(stream);
        lexer.InitializeTokens(IgnoreSpaceLexer.INDENT, IgnoreSpaceLexer.DEDENT, IgnoreSpaceLexer.EOL);
        var tokens = lexer.GetAllTokens();
        //_output.Write(lexer.TokenTypeMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key), tokens);
        Tokens.Assert(tokens, expectedTokens, nameof(IgnoreSpaceLexer));
    }

    private void AssertMultipleSpaceTokens(string source, int[] expectedTokens)
    {
        var stream = new AntlrInputStream(source);
        var lexer = new MultipleSpaceTokensLexer(stream);
        lexer.InitializeTokens(MultipleSpaceTokensLexer.INDENT, MultipleSpaceTokensLexer.DEDENT, MultipleSpaceTokensLexer.EOL);
        var tokens = lexer.GetAllTokens();
        //_output.Write(lexer.TokenTypeMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key), tokens);
        Tokens.Assert(tokens, expectedTokens, nameof(MultipleSpaceTokensLexer));
    }
}