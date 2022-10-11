using System.Collections.Generic;
using Antlr4.Runtime;
using FluentAssertions;

namespace Dentlr.UnitTests
{
    internal static class Tokens
    {
        public static void Assert(IList<IToken> lexedTokens, int[] expectedTokens)
        {
            int i = 0;
            foreach (var token in lexedTokens)
            {
                token.Type.Should().Be(expectedTokens[i], "at index {0}", i);
                i++;
            }
        }
    }
}
