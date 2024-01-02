using System.Collections.Generic;
using Antlr4.Runtime;
using FluentAssertions;
using Xunit.Abstractions;

namespace Dentlr.UnitTests;

internal static class Tokens
{
    public static void Assert(IList<IToken> lexedTokens, int[] expectedTokens, string source = "")
    {
        int i = 0;
        foreach (var token in lexedTokens)
        {
            token.Type.Should().Be(expectedTokens[i], "at index {0} ({1})", i, source);
            i++;
        }
    }

    public static void Write(this ITestOutputHelper output, IDictionary<int, string> tokenMap, IList<IToken> tokens)
    {
        foreach (var token in tokens)
        {
            var txt = token.Text;
            if (txt == "\n") txt = "\\n";
            if (txt == "\r\n") txt = "\\r\\n";
            output.WriteLine($"'{txt}' - {tokenMap[token.Type]} {token.Line}:{token.Column}");
        }
    }
}
