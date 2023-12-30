using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using FluentAssertions;
using Xunit;

namespace Dentlr.UnitTests.IgnoreSpace
{
    public class IgnoreSpaceTests
    {
        private const string EOL = "\n";
        private const string INDENT = "  ";

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
            
            var lexTokens = LexTokens(source);
            var tokens = new[]
            {
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.INDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.INDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.DEDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.DEDENT,
                IgnoreSpaceLexer.WORD,
            };

            Tokens.Assert(lexTokens, tokens);
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

            var lexTokens = LexTokens(source);
            var tokens = new[]
            {
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.INDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.DEDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.INDENT,
                IgnoreSpaceLexer.INDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.DEDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.DEDENT,
                IgnoreSpaceLexer.WORD,
            };

            Tokens.Assert(lexTokens, tokens);
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

            var lexTokens = LexTokens(source);
            var tokens = new[]
            {
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.INDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.INDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.DEDENT,
                IgnoreSpaceLexer.DEDENT,
                IgnoreSpaceLexer.WORD,
            };

            Tokens.Assert(lexTokens, tokens);
        }

        [Fact]
        public void TestLevels_EndDedent()
        {
            var source =
                "level0" + EOL +
                INDENT + "level1"
                ;

            var lexTokens = LexTokens(source);
            var tokens = new[]
            {
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.INDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.DEDENT
            };

            Tokens.Assert(lexTokens, tokens);
        }

        [Fact]
        public void TestLevels_RepeatIndent()
        {
            var source =
                "level0" + EOL +
                INDENT + "level1a" + EOL +
                INDENT + "level1b" + EOL
                ;

            var lexTokens = LexTokens(source);
            var tokens = new[]
            {
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.INDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.DEDENT
            };

            Tokens.Assert(lexTokens, tokens);
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

            var lexTokens = LexTokens(source);
            var tokens = new[]
            {
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.INDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.DEDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.INDENT,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.EOL,
                IgnoreSpaceLexer.WORD,
                IgnoreSpaceLexer.DEDENT
            };

            Tokens.Assert(lexTokens, tokens);
        }

        [Fact]
        public void TestLevels_InvalidIndentException()
        {
            var source =
                EOL +
                INDENT + INDENT + "level2" + EOL +      // indent size is set here
                INDENT + "level1"                       // indent length is too short!
                ;

            Action errorAction = () => LexTokens(source);
            errorAction.Should().Throw<InvalidIndentException>();
        }

        private static IList<IToken> LexTokens(string source)
        {
            var stream = new AntlrInputStream(source);
            var lexer = new IgnoreSpaceLexer(stream);
            lexer.InitializeTokens(IgnoreSpaceLexer.INDENT, IgnoreSpaceLexer.DEDENT, IgnoreSpaceLexer.EOL);
            return lexer.GetAllTokens();
        }
    }
}