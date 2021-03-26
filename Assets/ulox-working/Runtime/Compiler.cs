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

        public enum FunctionType
        {
            Script,
            Function,
            Method,
            Init,
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
            public bool isCaptured;
        }

        public class Upvalue
        {
            public byte index;
            public bool isLocal;
        }

        private class CompilerState
        {
            public Local[] locals = new Local[byte.MaxValue + 1];
            public Upvalue[] upvalues = new Upvalue[byte.MaxValue + 1];
            public int localCount;
            public int scopeDepth;
            public Chunk chunk;
            public CompilerState enclosing;
            public string currentClassName;
            public FunctionType functionType;
            public CompilerState(CompilerState enclosingState, FunctionType funcType) 
            { 
                enclosing = enclosingState;
                functionType = funcType;
            }
        }

        private string GetEnclosingClass()
        {
            for (int i = compilerStates.Count - 1; i >= 0; i--)
            {
                var cur = compilerStates[i].currentClassName;
                if (string.IsNullOrEmpty(cur))
                    continue;

                return cur;
            }

            return null;
        }

        private IndexableStack<CompilerState> compilerStates = new IndexableStack<CompilerState>();

        private Token currentToken, previousToken;
        private List<Token> tokens;
        private int tokenIndex;
        //todo if we have more than 1 compiler we want this to be static
        private ParseRule[] rules;

        private int CurrentChunkInstructinCount => CurrentChunk.instructions.Count;
        private Chunk CurrentChunk => compilerStates.Peek().chunk;

        public Compiler()
        {
            GenerateRules();
            PushCompilerState(string.Empty, FunctionType.Script);
        }

        public Chunk Compile(List<Token> inTokens)
        {
            tokens = inTokens;
            Advance();

            while (currentToken.TokenType != TokenType.EOF)
            {
                Declaration();
            }

            return EndCompile();
        }

        private void PushCompilerState(string name, FunctionType functionType)
        {
            compilerStates.Push(new CompilerState(compilerStates.Peek(), functionType)
            {
                chunk = new Chunk(name),
            });

            if (functionType == FunctionType.Method || functionType == FunctionType.Init)
                AddLocal(compilerStates.Peek(), "this",0);
            else
                AddLocal(compilerStates.Peek(), "", 0);
        }

        private void GenerateRules()
        {

            rules = new ParseRule[System.Enum.GetNames(typeof(TokenType)).Length];

            for (int i = 0; i < rules.Length; i++)
            {
                rules[i] = new ParseRule(null, null, Precedence.None);
            }

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
            rules[(int)TokenType.AND] = new ParseRule(null, And, Precedence.And);
            rules[(int)TokenType.OR] = new ParseRule(null, Or, Precedence.Or);
            rules[(int)TokenType.OPEN_PAREN] = new ParseRule(Grouping, Call, Precedence.Call);
            rules[(int)TokenType.DOT] = new ParseRule(null, Dot, Precedence.Call);
            rules[(int)TokenType.THIS] = new ParseRule(This, null, Precedence.None);
            rules[(int)TokenType.SUPER] = new ParseRule(Super, null, Precedence.None);
        }

        private void Super(bool obj)
        {
            if (GetEnclosingClass() == null)
                throw new CompilerException("Cannot use super outside a class.");
            //todo cannot use outisde a class without a super

            Consume(TokenType.DOT, "Expect '.' after a super.");
            Consume(TokenType.IDENTIFIER, "Expect superclass method name.");
            var nameID = AddStringConstant();

            NamedVariable("this", false);
            if (Match(TokenType.OPEN_PAREN))
            {
                byte argCount = ArgumentList();
                NamedVariable("super", false);
                EmitBytes((byte)OpCode.SUPER_INVOKE, nameID);
                EmitBytes(argCount);
            }
            else
            {
                NamedVariable("super", false);
                EmitBytes((byte)OpCode.GET_SUPER, nameID);
            }
        }

        private void This(bool obj)
        {
            if (GetEnclosingClass() == null)
                throw new CompilerException("Cannot use this outside of a class declaration.");

            Variable(false);
        }

        void Dot(bool canAssign)
        {
            Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
            byte name = AddStringConstant();

            if (canAssign && Match(TokenType.ASSIGN))
            {
                Expression();
                EmitBytes((byte)OpCode.SET_PROPERTY, name);
            }
            else if(Match(TokenType.OPEN_PAREN))
            {
                var argCount = ArgumentList();
                EmitBytes((byte)OpCode.INVOKE, name);
                EmitBytes(argCount);
            }
            else
            {
                EmitBytes((byte)OpCode.GET_PROPERTY, name);
            }
        }

        private void Call(bool canAssign)
        {
            var argCount = ArgumentList();
            EmitBytes((byte)OpCode.CALL, argCount);
        }

        private byte ArgumentList()
        {
            byte argCount = 0;
            if (!Check(TokenType.CLOSE_PAREN))
            {
                do
                {
                    Expression();
                    if (argCount == 255)
                        throw new CompilerException("Can't have more than 255 arguments.");
                    
                    argCount++;
                } while (Match(TokenType.COMMA));
            }

            Consume(TokenType.CLOSE_PAREN, "Expect ')' after arguments.");
            return argCount;
        }

        private void And(bool canAssign)
        {
            int endJump = EmitJump(OpCode.JUMP_IF_FALSE);

            EmitOpCode(OpCode.POP);
            ParsePrecedence(Precedence.And);

            PatchJump(endJump);
        }

        private void Or(bool canAssign)
        {
            int elseJump = EmitJump(OpCode.JUMP_IF_FALSE);
            int endJump = EmitJump(OpCode.JUMP);

            PatchJump(elseJump);
            EmitOpCode(OpCode.POP);

            ParsePrecedence(Precedence.Or);

            PatchJump(endJump);
        }

        private void Declaration()
        {
            if (Match(TokenType.CLASS))
                ClassDeclaration();
            else if (Match(TokenType.FUNCTION))
                FunctionDeclaration();
            else if (Match(TokenType.VAR))
                VarDeclaration();
            else
                Statement();
        }

        private void ClassDeclaration()
        {
            Consume(TokenType.IDENTIFIER, "Expect class name.");
            var className = (string)previousToken.Literal;
            compilerStates.Peek().currentClassName = className;
            byte nameConstant = AddStringConstant();
            DeclareVariable();

            EmitBytes((byte)OpCode.CLASS, nameConstant);
            DefineVariable(nameConstant);

            bool hasSuper = false;

            if(Match(TokenType.LESS))
            {
                Consume(TokenType.IDENTIFIER, "Expect superclass name.");
                Variable(false);
                if (className == (string)previousToken.Literal)
                    throw new CompilerException("A class cannot inhert from itself.");

                BeginScope();
                AddLocal(compilerStates.Peek(), "super");
                DefineVariable(0);

                NamedVariable(className, false);
                EmitOpCode(OpCode.INHERIT);
                hasSuper = true;
            }

            NamedVariable(className, false);
            Consume(TokenType.OPEN_BRACE, "Expect '{' before class body.");
            while (!Check(TokenType.CLOSE_BRACE) && !Check(TokenType.EOF))
            {
                Method();
            }
            Consume(TokenType.CLOSE_BRACE, "Expect '}' after class body.");
            EmitOpCode(OpCode.POP);

            if(hasSuper)
            {
                EndScope();
            }
        }

        private void Method()
        {
            Consume(TokenType.IDENTIFIER, "Expect method name.");
            byte constant = AddStringConstant();

            var name = CurrentChunk.constants[constant].val.asString;
            var funcType = FunctionType.Method;
            if (name == "init")
                funcType = FunctionType.Init;

            Function(name, funcType);
            EmitBytes((byte)OpCode.METHOD, constant);
        }

        private void FunctionDeclaration()
        {
            var global = ParseVariable("Expect function name.");
            MarkInitialised();

            Function(CurrentChunk.constants[global].val.asString, FunctionType.Function);
            DefineVariable(global);
        }

        private void Function(string name, FunctionType functionType)
        {
            PushCompilerState(name, functionType);

            BeginScope();
            var line = previousToken.Line;

            // Compile the parameter list.
            Consume(TokenType.OPEN_PAREN, "Expect '(' after function name.");
            if (!Check(TokenType.CLOSE_PAREN))
            {
                do
                {
                    CurrentChunk.Arity++;
                    if (CurrentChunk.Arity > 255)
                    {
                        throw new CompilerException("Can't have more than 255 parameters.");
                    }

                    var paramConstant = ParseVariable("Expect parameter name.");
                    DefineVariable(paramConstant);
                } while (Match(TokenType.COMMA));
            }
            Consume(TokenType.CLOSE_PAREN, "Expect ')' after parameters.");

            // The body.
            Consume(TokenType.OPEN_BRACE, "Expect '{' before function body.");
            Block();

            // Create the function object.
            var comp = compilerStates.Peek();   //we need this to mark upvalues
            var function = EndCompile();
            EmitBytes((byte)OpCode.CLOSURE, CurrentChunk.AddConstant(Value.New( function )));

            for (int i = 0; i < function.UpvalueCount; i++)
            {
                EmitBytes(comp.upvalues[i].isLocal ?  (byte)1 : (byte)0);
                EmitBytes(comp.upvalues[i].index);
            }
        }

        private void VarDeclaration()
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
            if (compilerStates.Peek().scopeDepth > 0) return 0;
            return IdentifierString();
        }

        private void DeclareVariable()
        {
            var comp = compilerStates.Peek();

            if (comp.scopeDepth == 0) return;

            var declName = comp.chunk.constants[AddStringConstant()].val.asString;

            for (int i = comp.localCount - 1; i >= 0; i--)
            {
                var local = comp.locals[i];
                if (local.depth != -1 && local.depth < comp.scopeDepth)
                    break;

                if (declName == local.name)
                    throw new CompilerException($"Already a variable with name '{declName}' in this scope.");
            }

            AddLocal(comp, declName);
        }

        private static void AddLocal(CompilerState comp, string name, int depth = -1)
        {
            if (comp.localCount == byte.MaxValue)
                throw new CompilerException("Too many local variables.");

            comp.locals[comp.localCount++] = new Local()
            {
                name = name,
                depth = depth
            };
        }

        private void DefineVariable(byte global)
        {
            if (compilerStates.Peek().scopeDepth > 0)
            {
                MarkInitialised();
                return;
            }

            EmitBytes((byte)OpCode.DEFINE_GLOBAL, global);
        }

        private void MarkInitialised()
        {
            var comp = compilerStates.Peek();

            if (comp.scopeDepth == 0) return;
            comp.locals[comp.localCount - 1].depth = comp.scopeDepth;
        }

        private void BeginScope()
        {
            compilerStates.Peek().scopeDepth++;
        }

        private void EndScope()
        {
            var comp = compilerStates.Peek();

            comp.scopeDepth--;

            while (comp.localCount > 0 &&
                comp.locals[comp.localCount - 1].depth > comp.scopeDepth)
            {
                if(comp.locals[comp.localCount - 1].isCaptured)
                    EmitOpCode(OpCode.CLOSE_UPVALUE);
                else
                    EmitOpCode(OpCode.POP);

                compilerStates.Peek().localCount--;
            }
        }

        private int ResolveUpvalue (CompilerState compilerState, string name)
        {
            if (compilerState.enclosing == null) return -1;

            int local = ResolveLocal(compilerState.enclosing, name);
            if (local != -1)
            {
                compilerState.enclosing.locals[local].isCaptured = true;
                return AddUpvalue(compilerState, (byte)local, true);
            }

            int upvalue = ResolveUpvalue(compilerState.enclosing, name);
            if (upvalue != -1)
            {
                return AddUpvalue(compilerState, (byte)upvalue, false);
            }

            return -1;
        }


        private int AddUpvalue(CompilerState compilerState, byte index, bool isLocal)
        {
            int upvalueCount = compilerState.chunk.UpvalueCount;

            Upvalue upvalue = default;

            for (int i = 0; i < upvalueCount; i++)
            {
                upvalue = compilerState.upvalues[i];
                if (upvalue.index == index && upvalue.isLocal == isLocal)
                {
                    return i;
                }
            }

            if (upvalueCount == byte.MaxValue)
            {
                throw new CompilerException("Too many closure variables in function.");
            }

            compilerState.upvalues[upvalueCount] = new Upvalue() { index = index, isLocal = isLocal };
            return compilerState.chunk.UpvalueCount++;
        }

        private int ResolveLocal(CompilerState compilerState, string name)
        {
            for (int i = compilerState.localCount - 1; i >= 0; i--)
            {
                var local = compilerState.locals[i];
                if (name == local.name)
                {
                    if (local.depth == -1)
                        throw new CompilerException("Cannot referenece a variable in it's own initialiser.");
                    return i;
                }
            }

            return -1;
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
            else if (Match(TokenType.RETURN))
            {
                ReturnStatement();
            }
            else if(Match(TokenType.WHILE))
            {
                WhileStatement();
            }
            else if (Match(TokenType.FOR))
            {
                ForStatement();
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

        private void ReturnStatement()
        {
            if (compilerStates.Count <= 1)
                throw new CompilerException("Cannot return from a top-level statement.");

            if (Match(TokenType.END_STATEMENT))
            {
                EmitReturn();
            }
            else if(compilerStates.Peek().functionType == FunctionType.Init)
            {
                throw new CompilerException("Cannot return an expression from an 'init'.");
            }
            else
            {
                Expression();
                Consume(TokenType.END_STATEMENT, "Expect ';' after return value.");
                EmitOpCode(OpCode.RETURN);
            }
        }

        private void WhileStatement()
        {
            int loopStart = CurrentChunkInstructinCount;

            Consume(TokenType.OPEN_PAREN, "Expect '(' after if.");
            Expression();
            Consume(TokenType.CLOSE_PAREN, "Expect ')' after if.");
            
            int exitJump = EmitJump(OpCode.JUMP_IF_FALSE);
            
            EmitOpCode(OpCode.POP);
            Statement();

            EmitLoop(loopStart);

            PatchJump(exitJump);
            EmitOpCode(OpCode.POP);
        }

        private void ForStatement()
        {
            BeginScope();

            Consume(TokenType.OPEN_PAREN, "Expect '(' after 'for'.");
            if (Match(TokenType.END_STATEMENT))
            {
                // No initializer.
            }
            else if (Match(TokenType.VAR))
            {
                VarDeclaration();
            }
            else
            {
                ExpressionStatement();
            }

            int loopStart = CurrentChunkInstructinCount;
            
            int exitJump = -1;
            if (!Match(TokenType.END_STATEMENT))
            {
                Expression();
                Consume(TokenType.END_STATEMENT, "Expect ';' after loop condition.");

                // Jump out of the loop if the condition is false.
                exitJump = EmitJump(OpCode.JUMP_IF_FALSE);
                EmitOpCode(OpCode.POP); // Condition.
            }

            if (!Match(TokenType.CLOSE_PAREN))
            {
                int bodyJump = EmitJump(OpCode.JUMP);

                int incrementStart = CurrentChunkInstructinCount;
                Expression();
                EmitOpCode(OpCode.POP);
                Consume(TokenType.CLOSE_PAREN, "Expect ')' after for clauses.");

                EmitLoop(loopStart);
                loopStart = incrementStart;
                PatchJump(bodyJump);
            }

            Statement();

            EmitLoop(loopStart);

            if (exitJump != -1)
            {
                PatchJump(exitJump);
                EmitOpCode(OpCode.POP); // Condition.
            }

            EndScope();
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
            int jump = CurrentChunkInstructinCount - thenjump - 2;

            if (jump > ushort.MaxValue)
                throw new CompilerException($"Cannot jump '{jump}'. Max jump is '{ushort.MaxValue}'");

            WriteBytesAt(thenjump, (byte)((jump >> 8) & 0xff), (byte)(jump & 0xff));
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
            NamedVariable((string)previousToken.Literal, canAssign);
        }

        private void NamedVariable(string name, bool canAssign)
        {
            //TODO if we already have the name don't store a dup

            OpCode getOp = OpCode.FETCH_GLOBAL, setOp = OpCode.ASSIGN_GLOBAL;
            var argID = ResolveLocal(compilerStates.Peek(), name);
            if (argID != -1)
            {
                getOp = OpCode.GET_LOCAL;
                setOp = OpCode.SET_LOCAL;
            }
            else
            {
                argID = ResolveUpvalue(compilerStates.Peek(), name);
                if (argID != -1)
                {
                    getOp = OpCode.GET_UPVALUE;
                    setOp = OpCode.SET_UPVALUE;
                }
                else
                {
                    argID = CurrentChunk.AddConstant(Value.New(name));
                }
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
            CurrentChunk.WriteConstant(Value.New((double)previousToken.Literal), previousToken.Line);
        }

        private void String(bool canAssign)
        {
            CurrentChunk.WriteConstant(Value.New((string)previousToken.Literal), previousToken.Line);
        }

        private byte IdentifierString()
        {
            return CurrentChunk.WriteConstant(Value.New((string)previousToken.Literal), previousToken.Line);
        }

        private byte AddStringConstant()
        {
            return CurrentChunk.AddConstant(Value.New((string)previousToken.Literal));
        }

        private Chunk EndCompile()
        {
            EmitReturn();
            return compilerStates.Pop().chunk;
        }

        private void EmitReturn()
        {
            if (compilerStates.Peek().functionType == FunctionType.Init)
                EmitBytes((byte)OpCode.GET_LOCAL, 0);
            else
                EmitOpCode(OpCode.NULL);
            
            EmitOpCode(OpCode.RETURN);
        }
        
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
            CurrentChunk.WriteSimple(op, previousToken.Line);
        }

        void EmitOpCodes(params OpCode[] op)
        {
            foreach (var item in op)
            {
                CurrentChunk.WriteSimple(item, previousToken.Line);
            }
        }

        void EmitBytes(params byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                CurrentChunk.WriteByte(b[i], previousToken.Line);
            }
        }

        void WriteBytesAt(int at, params byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                CurrentChunk.instructions[at+i] = b[i];
            }
        }

        private int EmitJump(OpCode op)
        {
            EmitBytes((byte)op, 0xff, 0xff);
            return CurrentChunk.instructions.Count - 2;
        }

        private void EmitLoop(int loopStart)
        {
            EmitOpCode(OpCode.LOOP);
            int offset = CurrentChunk.instructions.Count - loopStart + 2;

            if (offset > ushort.MaxValue)
                throw new CompilerException($"Cannot loop '{offset}'. Max loop is '{ushort.MaxValue}'");

            EmitBytes((byte)((offset >> 8) & 0xff),(byte)(offset & 0xff));
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
