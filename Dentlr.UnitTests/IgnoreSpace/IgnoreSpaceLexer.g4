lexer grammar IgnoreSpaceLexer;

tokens { INDENT, DEDENT }
options { superClass=Dentlr.DentlrLexer; }

WORD: [a-zA-Z0-9]+;

WS: [ \t] -> skip;
EOL: '\r'? '\n' | '\r';