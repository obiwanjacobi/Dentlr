using System;

namespace Dentlr;

public class InvalidIndentException : Exception
{
    public InvalidIndentException() { }
    public InvalidIndentException(string message) : base(message) { }
    public InvalidIndentException(string message, Exception inner) : base(message, inner) { }
}
