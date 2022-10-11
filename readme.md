# Dentlr

Dentlr is an `INDENT` and `DEDENT` token generating `Lexer` base class for Antlr4.

Dentlr can be used to parse positional grammars where leading whitespace determines scope level.
Languages like Python and F# use this syntax.

Dentlr implements a C# `Lexer` base class that detects Newline tokens 
and checks the indent of the following token. `INDENT` and `DEDENT` tokens are inserted in the tokens stream the `Lexer` emits.

Dentlr is dependent on: **Antlr4 9.2.0**

- Separate the Antlr lexer and parser grammar files. Put `options { tokenVocab=MyLexer; }` in the `parser grammer MyParser;` file.
- Detects indents only after a newline.
- The first indent encountered determines the indent size/length. All subsequent indents must be a multiple of that size or an `InvalidIndentException` is thrown.
- Use the IndentSize property to preset a fixed number of spaces to use for an `INDENT`.
- Any tokens that match whitespace are emitted after the `INDENT` and `DEDENT` tokens.
- Does not detect tabs `\t` (todo).

---

## Usage

In the lexer grammar file specify the tokens that will be used for the `INDENT` and `DEDENT` tokens using an `tokens {}` expression at the beginning of the file.
The name of these tokens does not matter. The tokens to be used by the `DentlrLexer` base class are initialized explicitly in code.

Next, specify the base (or super) class of the lexer class that will be generated for the lexer grammer file.
Using the expression `options { superClass=Dentlr.DentlrLexer; }` does just that.

Finally a `NEWLINE` (or `EOL`) token has to be defined the `DentlrLexer` uses to detect newlines that triggers indent (and dedent) recognition.

A typical lexer grammar file looks something like this:

```g4
lexer grammar MyLexer;

tokens { INDENT, DEDENT }
options { superClass=Dentlr.DentlrLexer; }

// need a newline (EndOfLine) token.
EOL: '\r'? '\n' | '\r';

// ... your tokens
```

There are two ways to initialize the tokens to be used by the `DentlrLexer`.
Either override the `NextToken` method to do a one-time initialization.

```csharp
public partial class MyLexer
{
    public override IToken? NextToken()
    {
        if (!AreTokensInitialized)
            InitializeTokens(INDENT, DEDENT, EOL);

        return base.NextToken();
    }
}
```

Or call `InitializeTokens()` at the time the `MyLexer` object is created.

```csharp
    string source ...;
    var stream = new AntlrInputStream(source);
    var lexer = new MyLexer(stream)
    {
        IndentSize = 4      // optional predetermined fixed indent size
    };
    lexer.InitializeTokens(MyLexer.INDENT, MyLexer.DEDENT, MyLexer.EOL);
    ...
```

An `InvalidOperationException` is thrown when the tokens are not initialized.

---

## TODO

- parse tabs / TabSize property
- invalid indent error mode (ignore, adjust, throw)
- Handle multiple sequential whitespace tokens (`Sp: ' ';`)
- Indent/Dedent token after or before Whitespace token? (before, after, remove)
- Implement a lexer base class for other languages (java, ts, python)
- Test if Dentlr can depend on the newest Antlr version while still allowing the unit tests to work.
- 