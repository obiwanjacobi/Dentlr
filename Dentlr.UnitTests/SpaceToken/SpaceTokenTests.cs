using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using FluentAssertions;
using Xunit;
using Zsharp.Parser;

namespace Dentlr.UnitTests.IgnoreSpace
{
    public class SpaceTokenTests
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
            
            var lexer = CreateLexer(source);
            var tokens = new[]
            {
                SpaceTokenLexer.WORD,
                SpaceTokenLexer.EOL,
                SpaceTokenLexer.INDENT,
                SpaceTokenLexer.WS,
                SpaceTokenLexer.WORD,
                SpaceTokenLexer.EOL,
                SpaceTokenLexer.INDENT,
                SpaceTokenLexer.WS,
                SpaceTokenLexer.WORD,
                SpaceTokenLexer.EOL,
                SpaceTokenLexer.DEDENT,
                SpaceTokenLexer.WS,
                SpaceTokenLexer.WORD,
                SpaceTokenLexer.EOL,
                SpaceTokenLexer.DEDENT,
                SpaceTokenLexer.WORD,
            };

            Tokens.Assert(lexer.GetAllTokens(), tokens);
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

            var lexer = CreateLexer(source);
            var tokens = new[]
            {
                SpaceTokenLexer.EOL,
                SpaceTokenLexer.INDENT,
                SpaceTokenLexer.WS,
                SpaceTokenLexer.WORD,
                SpaceTokenLexer.EOL,
                SpaceTokenLexer.DEDENT,
                SpaceTokenLexer.WORD,
                SpaceTokenLexer.EOL,
                SpaceTokenLexer.INDENT,
                SpaceTokenLexer.INDENT,
                SpaceTokenLexer.WS,
                SpaceTokenLexer.WORD,
                SpaceTokenLexer.EOL,
                SpaceTokenLexer.DEDENT,
                SpaceTokenLexer.WS,
                SpaceTokenLexer.WORD,
                SpaceTokenLexer.EOL,
                SpaceTokenLexer.DEDENT,
                SpaceTokenLexer.WORD,
            };

            Tokens.Assert(lexer.GetAllTokens(), tokens);
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

            var lexer = CreateLexer(source);
            var tokens = new[]
            {
                SpaceTokenLexer.WORD,
                SpaceTokenLexer.EOL,
                SpaceTokenLexer.INDENT,
                SpaceTokenLexer.WS,
                SpaceTokenLexer.WORD,
                SpaceTokenLexer.EOL,
                SpaceTokenLexer.INDENT,
                SpaceTokenLexer.WS,
                SpaceTokenLexer.WORD,
                SpaceTokenLexer.EOL,
                SpaceTokenLexer.DEDENT,
                SpaceTokenLexer.DEDENT,
                SpaceTokenLexer.WORD,
            };

            Tokens.Assert(lexer.GetAllTokens(), tokens);
        }

        [Fact]
        public void TestLevels_InvalidIndentException()
        {
            var source =
                EOL +
                INDENT + INDENT + "level2" + EOL +      // indent size is set here
                INDENT + "level1"                       // indent length is too short!
                ;

            var lexer = CreateLexer(source);
            Action errorAction = () => lexer.GetAllTokens();
            errorAction.Should().Throw<InvalidIndentException>();
        }

        private static SpaceTokenLexer CreateLexer(string source)
        {
            var stream = new AntlrInputStream(source);
            var lexer = new SpaceTokenLexer(stream);
            lexer.InitializeTokens(SpaceTokenLexer.INDENT, SpaceTokenLexer.DEDENT, SpaceTokenLexer.EOL);
            return lexer;
        }
    }
}