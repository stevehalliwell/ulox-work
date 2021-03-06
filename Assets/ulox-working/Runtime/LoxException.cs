﻿namespace ULox
{
    public class PanicException : System.Exception
    {
        public PanicException(string message = "") : base(message) { }
    }
 
    public class LoxException : System.Exception
    {
        public LoxException(string msg) : base(msg) { }

        public LoxException(TokenType tokenType, int line, int character, string msg)
            : base($"{tokenType}|{line}:{character} {msg}")
        { }
    }

    public class CompilerException : LoxException
    {
        public CompilerException(string msg) : base(msg) { }
    }

    public class VMException : LoxException
    {
        public VMException(string msg) : base(msg) { }
    }

    public class AssertException : LoxException
    {
        public AssertException(string msg) : base(msg) { }
    }

    public class LibraryException : LoxException
    {
        public LibraryException(string msg) : base(msg) { }
    }

    public class TestException : LoxException
    {
        public TestException(string msg) : base(msg) { }
    }

    public class TokenException : LoxException
    {
        public TokenException(Token token, string msg)
            : base(token.TokenType, token.Line, token.Character, msg)
        { }
    }

    public class ScannerException : LoxException
    {
        public ScannerException(TokenType tokenType, int line, int character, string msg)
            : base(tokenType, line, character, msg) { }
    }

    public class InstanceException : TokenException
    {
        public InstanceException(Token name, string msg) : base(name, msg) { }
    }

    public class EnvironmentException : TokenException
    {
        public EnvironmentException(Token token, string msg)
            : base(token, msg)
        { }
    }

    public class ResolverException : TokenException
    {
        public ResolverException(Token token, string msg) : base(token, msg) { }
    }

    public class ParseException : TokenException
    {
        public ParseException(Token token, string msg)
            : base(token, msg)
        { }
    }

    public class RuntimeException : TokenException
    {
        public RuntimeException(Token token, string msg)
            : base(token, msg)
        { }
    }

    public class RuntimeTypeException : RuntimeException
    {
        public RuntimeTypeException(Token token, string msg)
            : base(token, msg)
        { }
    }

    public class RuntimeCallException : RuntimeTypeException
    {
        public RuntimeCallException(Token token, string msg) : base(token, msg) { }
    }

    public class RuntimeAccessException : RuntimeTypeException
    {
        public RuntimeAccessException(Token token, string msg) : base(token, msg) { }
    }

    public class ClassException : TokenException
    {
        public ClassException(Token token, string msg) : base(token, msg) { }
    }

    public class InterpreterControlFlowException : System.Exception
    {
        public Token From { get; set; }

        public InterpreterControlFlowException(Token from)
        {
            From = from;
        }
    }
    public class Return : InterpreterControlFlowException
    {
        public object Value { get; set; }

        public Return(Token from, object val) : base(from)
        {
            Value = val;
        }
    }

    public class Break : InterpreterControlFlowException
    {
        public Break(Token from) : base(from) { }
    }

    public class Continue : InterpreterControlFlowException
    {
        public Continue(Token from) : base(from) { }
    }
}
