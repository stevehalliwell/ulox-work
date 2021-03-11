using System.Collections.Generic;

namespace ULox
{
    public class Compiler
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
            public System.Action<bool> prefix, infix;
            public Precedence precedence;

            public ParseRule(
                System.Action<bool> prefix,
                System.Action<bool> infix,
                Precedence pre)
            {
                this.prefix = prefix;
                this.infix = infix;
                this.precedence = pre;
            }
        }

        public class Local
        {
            public string name;
            public int depth;
        }

        private Local[] locals = new Local[byte.MaxValue + 1];
        private int localCount;
        private int scopeDepth;


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
            rules[(int)TokenType.IDENTIFIER] = new ParseRule(Variable, null, Precedence.None);
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
            if (Match(TokenType.VAR))
                VarStatement();
            else
                Statement();
        }

        private void VarStatement()
        {
            var global = ParseVariable("Expect variable name");

            if (Match(TokenType.ASSIGN))
                Expression();
            else
                EmitOpCode(OpCode.NULL);


            Consume(TokenType.END_STATEMENT, "Expect ; after variable declaration.");
            DefineVariable(global);
        }

        private byte ParseVariable(string errMsg)
        {
            Consume(TokenType.IDENTIFIER, errMsg);

            DeclareVariable();
            if (scopeDepth > 0) return 0;
            return IdentifierString();
        }

        private void DeclareVariable()
        {
            if (scopeDepth == 0) return;

            var declName = (string)previousToken.Literal;

            for (int i = localCount - 1; i >= 0; i--)
            {
                var local = locals[i];
                if (local.depth != -1 && local.depth < scopeDepth)
                    break;

                if (declName == local.name)
                    throw new CompilerException($"Already a variable with name '{declName}' in this scope.");
            }

            AddLocal(declName);
        }

        private void AddLocal(string name)
        {
            if (localCount == byte.MaxValue)
                throw new CompilerException("Too many local variables.");

            locals[localCount++] = new Local()
            {
                name = name,
                depth = -1
            };
        }

        private void DefineVariable(byte global)
        {
            if (scopeDepth > 0)
            {
                MarkInitialised();
                return;
            }

            EmitBytes((byte)OpCode.DEFINE_GLOBAL, global);
        }

        private void MarkInitialised()
        {
            locals[localCount - 1].depth = scopeDepth;
        }

        private void Statement()
        {
            if (Match(TokenType.PRINT))
            {
                PrintStatement();
            }
            else if (Match(TokenType.IF))
            {
                IfStatement();
            }
            else if (Match(TokenType.OPEN_BRACE))
            {
                BeginScope();
                Block();
                EndScope();
            }
            else
            {
                ExpressionStatement();
            }
        }

        private void IfStatement()
        {
            Consume(TokenType.OPEN_PAREN, "Expect '(' after if.");
            Expression();
            Consume(TokenType.CLOSE_PAREN, "Expect ')' after if.");

            int thenjump = EmitJump(OpCode.JUMP_IF_FALSE);
            EmitOpCode(OpCode.POP);

            Statement();

            int elseJump = EmitJump(OpCode.JUMP);

            PatchJump(thenjump);
            EmitOpCode(OpCode.POP);

            if (Match(TokenType.ELSE)) Statement();

            PatchJump(elseJump);
        }

        private void PatchJump(int thenjump)
        {
            int jump = currentChunk.instructions.Count - thenjump - 2;

            if (jump > ushort.MaxValue)
                throw new CompilerException($"Cannot jump '{jump}'. Max jump is '{ushort.MaxValue}'");

            currentChunk.instructions[thenjump] = (byte)((jump >> 8) & 0xff);
            currentChunk.instructions[thenjump + 1] = (byte)(jump & 0xff);
        }

        private void BeginScope()
        {
            scopeDepth++;
        }

        private void EndScope()
        {
            scopeDepth--;

            while(localCount > 0 &&
                locals[localCount -1].depth > scopeDepth)
            {
                EmitOpCode(OpCode.POP);
                localCount--;
            }
        }

        private void Block()
        {
            while (!Check(TokenType.CLOSE_BRACE) && !Check(TokenType.EOF))
                Declaration();

            Consume(TokenType.CLOSE_BRACE, "Expect '}' after block.");
        }

        private void ExpressionStatement()
        {
            Expression();
            Consume(TokenType.END_STATEMENT, "Expect ; after expression statement.");
            EmitOpCode(OpCode.POP);
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

        private void Grouping(bool canAssign)
        {
            Expression();
            Consume(TokenType.CLOSE_PAREN, "Expect ')' after expression.");
        }

        private void Variable(bool canAssign)
        {
            NamedVariable(canAssign);
        }

        private void NamedVariable(bool canAssign)
        {
            //TODO if we already have the name don't store a dup

            OpCode getOp = OpCode.FETCH_GLOBAL, setOp = OpCode.ASSIGN_GLOBAL;
            var argID = ResolveLocal((string)previousToken.Literal);
            if (argID != -1)
            {
                getOp = OpCode.FETCH_LOCAL;
                setOp = OpCode.ASSIGN_LOCAL;
            }
            else
            {
                argID = AddStringConstant();
            }

            if (canAssign && Match(TokenType.ASSIGN))
            {
                Expression();
                EmitBytes((byte)setOp, (byte)argID);
            }
            else
            {
                EmitBytes((byte)getOp, (byte)argID);
            }
        }

        private int ResolveLocal(string name)
        {
            for (int i = localCount - 1; i >= 0; i--)
            {
                var local = locals[i];
                if (name == local.name)
                {
                    if (local.depth == -1)
                        throw new CompilerException("Cannot referenece a variable in it's own initialiser.");
                    return i;
                }
            }

            return -1;
        }

        void Unary(bool canAssign)
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

        void Binary(bool canAssign)
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

        void Literal(bool canAssign)
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

        private void Number(bool canAssign)
        {
            currentChunk.WriteConstant(Value.New((double)previousToken.Literal), previousToken.Line);
        }

        private void String(bool canAssign)
        {
            currentChunk.WriteConstant(Value.New((string)previousToken.Literal), previousToken.Line);
        }

        private byte IdentifierString()
        {
            return currentChunk.WriteConstant(Value.New((string)previousToken.Literal), previousToken.Line);
        }

        private byte AddStringConstant()
        {
            return currentChunk.AddConstant(Value.New((string)previousToken.Literal));
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

        private int EmitJump(OpCode op)
        {
            EmitBytes((byte)op, 0xff, 0xff);
            return currentChunk.instructions.Count - 2;
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

            var canAssign = pre <= Precedence.Assignment;
            rule.prefix(canAssign);

            while (pre <= GetRule(currentToken.TokenType).precedence)
            {
                Advance();
                rule = GetRule(previousToken.TokenType);
                rule.infix(canAssign);
            }

            if (canAssign && Match(TokenType.ASSIGN))
            {
                throw new CompilerException("Invalid assignment target.");
            }
        }
    }
}
