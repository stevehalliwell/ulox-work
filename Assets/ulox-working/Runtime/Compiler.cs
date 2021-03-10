using System.Collections.Generic;

namespace ULox
{
    public enum Precedence
    {
        None,
        Assignment,
        Or,
        And,
        Equality,
        Comparison,
        Term,
        Factor,
        Unary,
        Call,
        Primary,
    }

    public class ParseRule
    {
        public System.Action prefix, infix;
        public Precedence precedence;

        public ParseRule(
            System.Action prefix,
            System.Action infix,
            Precedence pre)
        {
            this.prefix = prefix;
            this.infix = infix;
            this.precedence = pre;
        }
    }

    public class Compiler
    {
        private Token currentToken, previousToken;
        private List<Token> tokens;
        private int tokenIndex;
        private Chunk currentChunk;
        private ParseRule[] rules;

        public Compiler()
        {
            GenerateRules();
        }

        private void GenerateRules()
        {
            rules = new ParseRule[System.Enum.GetNames(typeof(TokenType)).Length];

            for (int i = 0; i < rules.Length; i++)
            {
                rules[i] = new ParseRule(null, null, Precedence.None);
            }

            rules[(int)TokenType.OPEN_BRACE] = new ParseRule(Grouping, null, Precedence.None);
            rules[(int)TokenType.MINUS] = new ParseRule(Unary, Binary, Precedence.Term);
            rules[(int)TokenType.PLUS] = new ParseRule(null, Binary, Precedence.Term);
            rules[(int)TokenType.SLASH] = new ParseRule(null, Binary, Precedence.Factor);
            rules[(int)TokenType.STAR] = new ParseRule(null, Binary, Precedence.Factor);
            rules[(int)TokenType.BANG] = new ParseRule(Unary, null, Precedence.None);
            rules[(int)TokenType.INT] = new ParseRule(Number, null, Precedence.None);
            rules[(int)TokenType.FLOAT] = new ParseRule(Number, null, Precedence.None);
            rules[(int)TokenType.TRUE] = new ParseRule(Literal, null, Precedence.None);
            rules[(int)TokenType.FALSE] = new ParseRule(Literal, null, Precedence.None);
            rules[(int)TokenType.NULL] = new ParseRule(Literal, null, Precedence.None);
            rules[(int)TokenType.BANG_EQUAL] = new ParseRule(null, Binary, Precedence.Equality);
            rules[(int)TokenType.EQUALITY] = new ParseRule(null, Binary, Precedence.Equality);
            rules[(int)TokenType.LESS ] = new ParseRule(null, Binary, Precedence.Comparison);
            rules[(int)TokenType.LESS_EQUAL] = new ParseRule(null, Binary, Precedence.Comparison);
            rules[(int)TokenType.GREATER] = new ParseRule(null, Binary, Precedence.Comparison);
            rules[(int)TokenType.GREATER_EQUAL] = new ParseRule(null, Binary, Precedence.Comparison);
            rules[(int)TokenType.STRING] = new ParseRule(String, null, Precedence.None);
        }

        public bool Compile(Chunk chunk, List<Token> inTokens)
        {
            tokens = inTokens;
            currentChunk = chunk;
            Advance();

            while (currentToken.TokenType != TokenType.EOF)
            {
                Declaration();
            }

            EndCompile();
            return true;
        }

        private void Declaration()
        {
            Statement();
        }
        
        private void Statement()
        {
            if (Match(TokenType.PRINT))
                PrintStatement();
        }

        private void Expression()
        {
            ParsePrecedence(Precedence.Assignment);
        }

        void PrintStatement()
        {
            Expression();
            Consume(TokenType.END_STATEMENT, "Expect ; after print statement.");
            EmitOpCode(OpCode.PRINT);
        }

        private void Grouping()
        {
            Expression();
            Consume(TokenType.CLOSE_PAREN, "Expect ')' after expression.");
        }

        void Unary()
        {
            var op = previousToken.TokenType;

            ParsePrecedence(Precedence.Unary);

            switch (op)
            {
            case TokenType.MINUS: EmitOpCode(OpCode.NEGATE); break;
            case TokenType.BANG: EmitOpCode(OpCode.NOT); break;
            default:
                break;
            }
        }

        void Binary()
        {
            TokenType operatorType = previousToken.TokenType;

            // Compile the right operand.
            ParseRule rule = GetRule(operatorType);
            ParsePrecedence((Precedence)(rule.precedence + 1));

            switch(operatorType)
            {
            case TokenType.PLUS:          EmitOpCode(OpCode.ADD);       break;
            case TokenType.MINUS:         EmitOpCode(OpCode.SUBTRACT);  break;
            case TokenType.STAR:          EmitOpCode(OpCode.MULTIPLY);  break;
            case TokenType.SLASH:         EmitOpCode(OpCode.DIVIDE);    break;
            case TokenType.EQUALITY:      EmitOpCode(OpCode.EQUAL);     break;
            case TokenType.GREATER:       EmitOpCode(OpCode.GREATER);   break;
            case TokenType.LESS:          EmitOpCode(OpCode.LESS);      break;
            case TokenType.BANG_EQUAL:    EmitOpCodes(OpCode.EQUAL, OpCode.NOT);    break;
            case TokenType.GREATER_EQUAL: EmitOpCodes(OpCode.GREATER, OpCode.NOT);  break;
            case TokenType.LESS_EQUAL:    EmitOpCodes(OpCode.LESS, OpCode.NOT);     break;

            default:
                break;
            }

        }

        void Literal()
        {
            switch(previousToken.TokenType)
            {
            case TokenType.TRUE:  EmitOpCode(OpCode.TRUE);  break;
            case TokenType.FALSE: EmitOpCode(OpCode.FALSE); break;
            case TokenType.NULL:  EmitOpCode(OpCode.NULL);  break;
            }
        }

        private ParseRule GetRule(TokenType operatorType)
        {
            return rules[(int)operatorType];
        }

        void Number()
        {
            currentChunk.WriteConstant(Value.New((double)previousToken.Literal), previousToken.Line);
        }

        void String()
        {
            currentChunk.WriteConstant(Value.New((string)previousToken.Literal), previousToken.Line);
        }

        private void EndCompile() => EmitReturn();

        private void EmitReturn() => EmitOpCode(OpCode.RETURN);

        void Consume(TokenType tokenType, string msg)
        {
            if (currentToken.TokenType == tokenType)
                Advance();
            else
                throw new CompilerException(msg);
        }

        bool Check(TokenType type)
        {
            return currentToken.TokenType == type;
        }

        bool Match(TokenType type)
        {
            if (!Check(type))
                return false;
            Advance();
            return true;
        }

        void EmitOpCode(OpCode op)
        {
            currentChunk.WriteSimple(op, previousToken.Line);
        }

        void EmitOpCodes(params OpCode[] op)
        {
            foreach (var item in op)
            {
                currentChunk.WriteSimple(item, previousToken.Line);
            }
        }

        void EmitBytes(params byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                currentChunk.WriteByte(b[i], previousToken.Line);
            }
        }

        private void Advance()
        {
            previousToken = currentToken;
            currentToken = tokens[tokenIndex];
            tokenIndex++;
        }

        void ParsePrecedence(Precedence pre)
        {
            Advance();
            var rule = GetRule(previousToken.TokenType);
            if(rule.prefix == null)
            {
                throw new CompilerException("Expected prefix handler, but got null.");
            }

            rule.prefix();

            while (pre <= GetRule(currentToken.TokenType).precedence)
            {
                Advance();
                rule = GetRule(previousToken.TokenType);
                rule.infix();
            }
        }
    }
}
