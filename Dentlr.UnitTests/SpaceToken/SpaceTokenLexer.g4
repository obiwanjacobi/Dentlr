lexer grammar SpaceTokenLexer;

tokens { INDENT, DEDENT }
options { superClass=Dentlr.DentlrLexer; }

WORD: [a-zA-Z0-9]+;

WS: [ \t]+;
EOL: '\r'? '\n' | '\r';