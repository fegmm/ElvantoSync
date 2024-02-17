using System;

namespace ElvantoSync.Exceptions;

public class ContainsDataException : Exception
{
    public ContainsDataException() { }

    public ContainsDataException(string message) : base(message) { }
}
