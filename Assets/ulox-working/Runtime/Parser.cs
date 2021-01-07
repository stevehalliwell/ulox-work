using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public class Parser
    {
        private enum FunctionType { None, Function, Method, Get, Set, }

        private List<Token> _tokens;
        private int current = 0;
        public bool CatchAndSynch { get; set; } = true;
        private int _loopDepth;
        public Token _currentClassToken;

        public List<Stmt> Parse(List<Token> tokens)
        {
            _tokens = tokens;
            var statements = new List<Stmt>();
            try
            {
                while (!IsAtEnd())
                {
                    statements.Add(Declaration());
                }
            }
            catch (ParseException exception)
            {
                if (CatchAndSynch)
                    return null;
                else
                    throw;
            }
            return statements;
        }

        private Stmt Declaration()
        {
            try
            {
                if (Match(TokenType.CLASS)) return ClassDeclaration();
                if (Check(TokenType.FUNCTION) && CheckNext(TokenType.IDENTIFIER))
                {
                    Consume(TokenType.FUNCTION, null);
                    return Function(FunctionType.Function);
                }
                if (Match(TokenType.VAR)) return VarDeclaration();

                return Statement();
            }
            catch (ParseException exception)
            {
                if (CatchAndSynch)
                {
                    Synchronize();
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        private Stmt ClassDeclaration()
        {
            var className = Consume(TokenType.IDENTIFIER, "Expect class name.");

            var previousClassToken = _currentClassToken;
            _currentClassToken = className;

            Expr.Variable superclass = null;
            if (Match(TokenType.LESS))
            {
                Consume(TokenType.IDENTIFIER, "Expect superclass name.");
                superclass = new Expr.Variable(Previous(),EnvironmentVariableLocation.Invalid);
            }

            Consume(TokenType.OPEN_BRACE, "Expect { befefore class body.");

            var methods = new List<Stmt.Function>();
            var metaMethods = new List<Stmt.Function>();
            var fields = new List<Stmt.Var>();
            var metaFields = new List<Stmt.Var>();

            while (!Check(TokenType.CLOSE_BRACE) && !IsAtEnd())
            {
                var funcType = FunctionType.Method;
                var isClassMeta = Match(TokenType.CLASS);
                if (Match(TokenType.GET))
                {
                    //check if its short hand and generate, if not then pass to function
                    if (CheckNext(TokenType.END_STATEMENT, 
                        TokenType.ASSIGN,
                        TokenType.COMMA))
                    {
                        HandleGetSetGeneration(
                            className, 
                            methods, 
                            metaMethods, 
                            fields, 
                            metaFields, 
                            isClassMeta,
                            true, 
                            false);
                    }
                    else
                    {
                        var genFunc = Function(FunctionType.Get);
                        (isClassMeta ? metaMethods : methods).Add(genFunc);
                    }
                }
                else if (Match(TokenType.SET))
                {
                    if (CheckNext(TokenType.END_STATEMENT,
                       TokenType.ASSIGN,
                       TokenType.COMMA))
                    {
                        HandleGetSetGeneration(
                            className,
                            methods,
                            metaMethods,
                            fields,
                            metaFields,
                            isClassMeta,
                            false,
                            true);
                    }
                    else
                    {
                        var genFunc = Function(FunctionType.Set);
                        (isClassMeta ? metaMethods : methods).Add(genFunc);
                    }
                }
                else if (Match(TokenType.GETSET))
                {
                    HandleGetSetGeneration(
                            className,
                            methods,
                            metaMethods,
                            fields,
                            metaFields,
                            isClassMeta,
                            true,
                            true);
                }
                else if (Match(TokenType.VAR))
                {
                    var varStatements = FlattenChainOfVars(VarDeclaration());
                    foreach (var varStatement in varStatements)
                    {
                        (isClassMeta ? metaFields : fields).Add(varStatement);
                    }
                }
                else
                {
                    (isClassMeta ? metaMethods : methods).Add(Function(funcType));
                    //throw new ParseException(Peek(), "Unexpected token in class declaration.");
                }
            }

            //validate duplicate methods
            foreach (var method in methods)
            {
                if (methods.Count(x => x.name.Lexeme == method.name.Lexeme) > 1)
                    throw new ClassException(method.name,
                        $"Classes cannot have methods of identical names. Found more than 1 {method.name.Lexeme} in class {className.Lexeme}.");

                if (fields.Count(x => x.name.Lexeme == method.name.Lexeme) > 0)
                    throw new ClassException(method.name,
                        $"Classes cannot have a field and a method of identical names. Found more than 1 {method.name.Lexeme} in class {className.Lexeme}.");
            }

            foreach (var method in metaMethods)
            {
                if (metaMethods.Count(x => x.name.Lexeme == method.name.Lexeme) > 1)
                    throw new ClassException(method.name,
                        $"Classes cannot have metaMethods of identical names. Found more than 1 {method.name.Lexeme} in class {className.Lexeme}.");

                if (metaFields.Count(x => x.name.Lexeme == method.name.Lexeme) > 0)
                    throw new ClassException(method.name,
                        $"Classes cannot have a metaFields and a metaMethods of identical names. Found more than 1 {method.name.Lexeme} in class {className.Lexeme}.");
            }

            foreach (var field in fields)
            {
                if (fields.Count(x => x.name.Lexeme == field.name.Lexeme) > 1)
                    throw new ClassException(field.name,
                        $"Classes cannot have fields of identical names. Found more than 1 {field.name.Lexeme} in class {className.Lexeme}.");
            }

            foreach (var field in metaFields)
            {
                if (metaFields.Count(x => x.name.Lexeme == field.name.Lexeme) > 1)
                    throw new ClassException(field.name,
                        $"Classes cannot have metaFields of identical names. Found more than 1 {field.name.Lexeme} in class {className.Lexeme}.");
            }

            Consume(TokenType.CLOSE_BRACE, "Expect } after class body.");

            _currentClassToken = previousClassToken;

            return new Stmt.Class(
                className,
                EnvironmentVariableLocation.InvalidSlot,
                superclass,
                methods,
                metaMethods,
                fields,
                metaFields);
        }

        private void HandleGetSetGeneration(
            Token className, 
            List<Stmt.Function> methods, 
            List<Stmt.Function> metaMethods, 
            List<Stmt.Var> fields, 
            List<Stmt.Var> metaFields, 
            bool isClassMeta,
            bool doGet,
            bool doSet)
        {
            var origAssigns = FlattenChainOfVars(VarDeclaration());
            foreach (var origAssign in origAssigns)
            {
                var hiddenAssign = ToClassAutoVarDeclaration(origAssign);
                if (doGet)
                {
                    var genFunc = CreateGetMethod(className, origAssign.name, hiddenAssign.name);
                    (isClassMeta ? metaMethods : methods).Add(genFunc);
                }
                if(doSet)
                {
                    var genFunc = CreateSetMethod(className, origAssign.name, hiddenAssign.name);
                    (isClassMeta ? metaMethods : methods).Add(genFunc);
                }
                (isClassMeta ? metaFields : fields).Add(hiddenAssign);
            }
        }

        private List<Stmt.Var> FlattenChainOfVars(Stmt stmt)
        {
            var res = new List<Stmt.Var>();

            FlattenChainOfVars(stmt, res);

            return res;
        }

        private void FlattenChainOfVars(Stmt stmt, List<Stmt.Var> res)
        {
            if(stmt is Stmt.Var isVar)
            {
                res.Add(isVar);
            }
            else if (stmt is Stmt.Chain isChain)
            {
                FlattenChainOfVars(isChain.left, res);
                FlattenChainOfVars(isChain.right, res);
            }
        }

        private static Stmt.Function CreateSetMethod(Token name, Token writtenFieldName, Token hiddenInternalFieldName)
        {
            var setFuncName = writtenFieldName.Copy(TokenType.IDENTIFIER, "Set" + writtenFieldName.Lexeme);
            var valueName = writtenFieldName.Copy(TokenType.IDENTIFIER, "value");
            return new Stmt.Function(setFuncName,
                new Expr.Function(new List<Token>() { valueName },
                    new List<Stmt>()
                    {
                        new Stmt.Expression(new Expr.Set(
                            new Expr.This(name.Copy(TokenType.THIS, "this"), EnvironmentVariableLocation.Invalid),
                            hiddenInternalFieldName,
                            new Expr.Variable(valueName, EnvironmentVariableLocation.Invalid)))
                    },false, false), 
                EnvironmentVariableLocation.InvalidSlot);
        }

        private static Stmt.Function CreateGetMethod(Token className, Token writtenFieldName, Token hiddenInternalFieldName)
        {
            return new Stmt.Function(writtenFieldName,
                new Expr.Function(null,
                    new List<Stmt>()
                    {
                        new Stmt.Return(className.Copy(TokenType.RETURN), 
                        new Expr.Get(
                            new Expr.This(className.Copy(TokenType.THIS, "this"), 
                                EnvironmentVariableLocation.Invalid), hiddenInternalFieldName, EnvironmentVariableLocation.Invalid))
                    }, false, false)
                , EnvironmentVariableLocation.InvalidSlot);
        }

        private Stmt.Function Function(FunctionType functionType)
        {
            var name = Consume(TokenType.IDENTIFIER, $"Expect {functionType} name.");
            return new Stmt.Function(name, FunctionBody(functionType), EnvironmentVariableLocation.InvalidSlot);
        }

        private Expr.Function FunctionBody(FunctionType functionType)
        {
            List<Token> parameters = null;

            if (functionType == FunctionType.Function || Check(TokenType.OPEN_PAREN))
            {
                parameters = new List<Token>();
                Consume(TokenType.OPEN_PAREN, $"Expect '(' after {functionType} name.");
                if (!Check(TokenType.CLOSE_PAREN))
                {
                    do
                    {
                        if (parameters.Count >= 255)
                        {
                            throw new ParseException(Peek(), "Can't have more than 255 arguments.");
                        }

                        parameters.Add(Consume(TokenType.IDENTIFIER, "Expect parameter name."));
                    } while (Match(TokenType.COMMA));
                }
                Consume(TokenType.CLOSE_PAREN, "Expect ')' after parameters.");
            }

            if (functionType == FunctionType.Get &&
                (parameters != null && parameters.Count != 0))
            {
                throw new ClassException(_currentClassToken, "Cannot have arguments to a Get.");
            }

            if (functionType == FunctionType.Set &&
                (parameters != null && parameters.Count != 0))
            {
                throw new ClassException(_currentClassToken, "Cannot have arguments to a Set. A 'value' param is auto generated.");
            }

            if (functionType == FunctionType.Set)
            {
                var createdValueParam = Previous().Copy(TokenType.IDENTIFIER, "value");
                parameters = new List<Token>();
                parameters.Add(createdValueParam);
            }

            Consume(TokenType.OPEN_BRACE, $"Expect '{{' before {functionType} body.");
            var body = Block();
            return new Expr.Function(parameters, body, false, false);
        }

        private static Stmt.Var ToClassAutoVarDeclaration(Stmt.Var inVar)
        {
            return new Stmt.Var(inVar.name.Copy(inVar.name.TokenType, "_" + inVar.name.Lexeme), inVar.initializer, EnvironmentVariableLocation.InvalidSlot);
        }

        private Stmt VarDeclaration()
        {
            Token name = Consume(TokenType.IDENTIFIER, "Expect variable name.");

            Expr initializer = null;
            if (Match(TokenType.ASSIGN))
            {
                initializer = Expression();
            }
            
            if (Match(TokenType.COMMA))
            {
                return new Stmt.Chain(
                    new Stmt.Var(name, initializer, EnvironmentVariableLocation.InvalidSlot),
                    VarDeclaration());
            }
            else
            {
                Consume(TokenType.END_STATEMENT, "Expect end of statement after variable declaration.");
                return new Stmt.Var(name, initializer, EnvironmentVariableLocation.InvalidSlot);
            }
        }

        private Stmt Statement()
        {
            if (Match(TokenType.LOOP)) return LoopStatement();
            if (Match(TokenType.FOR)) return ForStatement();
            if (Match(TokenType.IF)) return IfStatement();
            if (Match(TokenType.PRINT)) return PrintStatement();
            if (Match(TokenType.RETURN)) return ReturnStatement();
            if (Match(TokenType.WHILE)) return WhileStatement();
            if (Match(TokenType.BREAK)) return BreakStatement();
            if (Match(TokenType.CONTINUE)) return ContinueStatement();
            if (Match(TokenType.OPEN_BRACE)) return new Stmt.Block(Block());

            return ExpressionStatement();
        }

        private Stmt ReturnStatement()
        {
            var keyword = Previous();
            Expr value = null;
            if (!Check(TokenType.END_STATEMENT))
                value = Expression();

            Consume(TokenType.END_STATEMENT, "Expect ; after return value.");
            return new Stmt.Return(keyword, value);
        }

        private Stmt BreakStatement()
        {
            var keyword = Previous();
            if (_loopDepth == 0)
            {
                throw new ParseException(keyword, "Cannot break when not within a loop.");
            }
            Consume(TokenType.END_STATEMENT, "Expect ; after break.");
            return new Stmt.Break(keyword);
        }

        private Stmt ContinueStatement()
        {
            var keyword = Previous();
            if (_loopDepth == 0)
            {
                throw new ParseException(keyword, "Cannot continue when not within a loop.");
            }
            Consume(TokenType.END_STATEMENT, "Expect ; after continue.");
            return new Stmt.Continue(keyword);
        }

        private Stmt ForStatement()
        {
            Consume(TokenType.OPEN_PAREN, "Expect '(' after 'for'.");

            Stmt initializer;
            if (Match(TokenType.END_STATEMENT))
            {
                initializer = null;
            }
            else if (Match(TokenType.VAR))
            {
                initializer = VarDeclaration();
            }
            else
            {
                initializer = ExpressionStatement();
            }

            Expr condition = null;
            if (!Check(TokenType.END_STATEMENT))
            {
                condition = Expression();
            }
            Consume(TokenType.END_STATEMENT, "Expect ';' after loop condition.");

            Expr increment = null;
            if (!Check(TokenType.CLOSE_PAREN))
            {
                increment = Expression();
            }
            Consume(TokenType.CLOSE_PAREN, "Expect ')' after for clauses.");

            _loopDepth++;
            Stmt body = Statement();

            if (condition == null) condition = new Expr.Literal(true);
            body = new Stmt.While(condition, body, new Stmt.Expression(increment));

            if (initializer != null)
            {
                body = new Stmt.Block(new List<Stmt>() { initializer, body });
            }

            _loopDepth--;
            return body;
        }

        private Stmt WhileStatement()
        {
            _loopDepth++;
            Consume(TokenType.OPEN_PAREN, "Expect '(' after 'while'.");
            Expr condition = Expression();
            Consume(TokenType.CLOSE_PAREN, "Expect ')' after condition.");
            Stmt body = Statement();
            _loopDepth--;
            return new Stmt.While(condition, body, null);
        }

        private Stmt LoopStatement()
        {
            _loopDepth++;
            Stmt body = Statement();
            _loopDepth--;
            return new Stmt.While(new Expr.Literal(true), body, null);
        }

        private Stmt IfStatement()
        {
            Consume(TokenType.OPEN_PAREN, "Expect '(' after 'if'.");
            Expr condition = Expression();
            Consume(TokenType.CLOSE_PAREN, "Expect ')' after if condition.");

            Stmt thenBranch = Statement();
            Stmt elseBranch = null;
            if (Match(TokenType.ELSE))
            {
                elseBranch = Statement();
            }

            return new Stmt.If(condition, thenBranch, elseBranch);
        }

        private List<Stmt> Block()
        {
            var statements = new List<Stmt>();

            while (!IsAtEnd() && !Check(TokenType.CLOSE_BRACE))
                statements.Add(Declaration());

            Consume(TokenType.CLOSE_BRACE, "Expect '}' after block.");
            return statements;
        }

        private Stmt PrintStatement()
        {
            var value = Expression();
            Consume(TokenType.END_STATEMENT, "Expect end of statement after value.");
            return new Stmt.Print(value);
        }

        private Stmt ExpressionStatement()
        {
            var expr = Expression();
            Consume(TokenType.END_STATEMENT, "Expect end of statement after value.");
            return new Stmt.Expression(expr);
        }

        private Expr Expression() => Conditional();

        internal void Reset()
        {
            _tokens = null;
            current = 0;
            _loopDepth = 0;
        }

        private Expr Assignment()
        {
            Expr expr = Or();

            bool compound = false;

            if (Match(
                TokenType.MINUS_EQUAL,
                TokenType.PLUS_EQUAL,
                TokenType.STAR_EQUAL,
                TokenType.SLASH_EQUAL,
                TokenType.PERCENT_EQUAL))
            {
                compound = true;
            }

            if (Match(TokenType.ASSIGN) || compound)
            {
                Token equals = Previous();
                Expr value = Assignment();

                if (expr is Expr.Variable varExpr)
                {
                    Token name = varExpr.name;
                    switch (equals.TokenType)
                    {
                        case TokenType.MINUS_EQUAL: return new Expr.Assign(name, new Expr.Binary(expr, equals.Copy(TokenType.MINUS), value), EnvironmentVariableLocation.Invalid);
                        case TokenType.PLUS_EQUAL: return new Expr.Assign(name, new Expr.Binary(expr, equals.Copy(TokenType.PLUS), value), EnvironmentVariableLocation.Invalid);
                        case TokenType.STAR_EQUAL: return new Expr.Assign(name, new Expr.Binary(expr, equals.Copy(TokenType.STAR), value), EnvironmentVariableLocation.Invalid);
                        case TokenType.SLASH_EQUAL: return new Expr.Assign(name, new Expr.Binary(expr, equals.Copy(TokenType.SLASH), value), EnvironmentVariableLocation.Invalid);
                        case TokenType.PERCENT_EQUAL: return new Expr.Assign(name, new Expr.Binary(expr, equals.Copy(TokenType.PERCENT), value), EnvironmentVariableLocation.Invalid);
                        case TokenType.ASSIGN: return new Expr.Assign(name, value, EnvironmentVariableLocation.Invalid);
                    }
                }
                else if (expr is Expr.Get exprGet)
                {
                    Expr obj = exprGet.obj;
                    Token name = exprGet.name;
                    switch (equals.TokenType)
                    {
                        case TokenType.MINUS_EQUAL: return new Expr.Set(obj, name, new Expr.Binary(expr, equals.Copy(TokenType.MINUS), value));
                        case TokenType.PLUS_EQUAL: return new Expr.Set(obj, name, new Expr.Binary(expr, equals.Copy(TokenType.PLUS), value));
                        case TokenType.STAR_EQUAL: return new Expr.Set(obj, name, new Expr.Binary(expr, equals.Copy(TokenType.STAR), value));
                        case TokenType.SLASH_EQUAL: return new Expr.Set(obj, name, new Expr.Binary(expr, equals.Copy(TokenType.SLASH), value));
                        case TokenType.PERCENT_EQUAL: return new Expr.Set(obj, name, new Expr.Binary(expr, equals.Copy(TokenType.PERCENT), value));
                        case TokenType.ASSIGN: return new Expr.Set(obj, name, value);
                    }
                }

                //a 'super.a = 1;' ends up in here unhandled

                throw new ParseException(equals, "Invalid assignment target.");
            }

            return expr;
        }

        private Expr Or()
        {
            Expr expr = And();

            while (Match(TokenType.OR))
            {
                Token op = Previous();
                Expr right = And();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }

        private Expr And()
        {
            Expr expr = Equality();

            while (Match(TokenType.AND))
            {
                Token op = Previous();
                Expr right = Equality();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }

        private Expr Equality()
        {
            var expr = Comparison();

            while (Match(TokenType.BANG_EQUAL, TokenType.EQUALITY))
            {
                Token op = Previous();
                Expr right = Comparison();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Comparison()
        {
            Expr expr = Term();

            while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                Token op = Previous();
                Expr right = Term();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Term()
        {
            Expr expr = Factor();

            while (Match(TokenType.MINUS, TokenType.PLUS, TokenType.PERCENT))
            {
                Token op = Previous();
                Expr right = Factor();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Factor()
        {
            Expr expr = Unary();

            while (Match(TokenType.SLASH, TokenType.STAR))
            {
                Token op = Previous();
                Expr right = Unary();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Unary()
        {
            if (Match(TokenType.BANG, TokenType.MINUS))
            {
                Token op = Previous();
                Expr right = Unary();
                return new Expr.Unary(op, right);
            }

            return Call();
        }

        private Expr Call()
        {
            Expr expr = Primary();

            while (true)
            {
                if (Match(TokenType.OPEN_PAREN))
                {
                    expr = FinishCall(expr);
                }
                else if (Match(TokenType.DOT))
                {
                    var name = Consume(TokenType.IDENTIFIER,
                        "Expect property name after '.'.");
                    expr = new Expr.Get(expr, name, EnvironmentVariableLocation.Invalid);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private Expr FinishCall(Expr callee)
        {
            List<Expr> arguments = new List<Expr>();
            if (!Check(TokenType.CLOSE_PAREN))
            {
                do
                {
                    if (arguments.Count >= 255)
                    {
                        throw new ParseException(Peek(), "Can't have more than 255 arguments.");
                    }
                    arguments.Add(Expression());
                } while (Match(TokenType.COMMA));
            }

            Token paren = Consume(TokenType.CLOSE_PAREN, "Expect ')' after arguments.");

            return new Expr.Call(callee, paren, arguments);
        }

        private Expr Primary()
        {
            if (Match(TokenType.FUNCTION)) return FunctionBody(FunctionType.Function);
            if (Match(TokenType.FALSE)) return new Expr.Literal(false);
            if (Match(TokenType.TRUE)) return new Expr.Literal(true);
            if (Match(TokenType.NULL)) return new Expr.Literal(null);

            if (Match(TokenType.INT, TokenType.FLOAT, TokenType.STRING))
            {
                return new Expr.Literal(Previous().Literal);
            }

            if (Match(TokenType.SUPER))
            {
                Token keyword = Previous();
                Token specifiedClass = new Token();
                if(Match(TokenType.OPEN_PAREN))
                {
                    specifiedClass = Consume(TokenType.IDENTIFIER,
                    "Expect parent class identifiying token.");
                    Consume(TokenType.CLOSE_PAREN, "Expect ')' after 'super' with specified parent class name.");
                }
                Consume(TokenType.DOT, "Expect '.' after 'super'.");
                Token method = Consume(TokenType.IDENTIFIER,
                    "Expect superclass method name.");
                return new Expr.Super(
                    keyword, 
                    specifiedClass, 
                    method, 
                    EnvironmentVariableLocation.Invalid, 
                    EnvironmentVariableLocation.Invalid);
            }

            if (Match(TokenType.THIS)) return new Expr.This(Previous(), EnvironmentVariableLocation.Invalid);

            if (Match(TokenType.IDENTIFIER))
            {
                return new Expr.Variable(Previous(), EnvironmentVariableLocation.Invalid);
            }

            if (Match(TokenType.OPEN_PAREN))
            {
                Expr expr = Expression();
                Consume(TokenType.CLOSE_PAREN, "Expect ')' after expression.");
                return new Expr.Grouping(expr);
            }

            if (Match(TokenType.ASSIGN,
                TokenType.GREATER,
                TokenType.GREATER_EQUAL,
                TokenType.LESS,
                TokenType.LESS_EQUAL,
                TokenType.PLUS,
                TokenType.SLASH,
                TokenType.STAR,
                TokenType.QUESTION))
            {
                throw new ParseException(Previous(), "Missing left-had operand.");
            }

            if (Match(TokenType.GET,
                TokenType.SET,
                TokenType.GETSET))
            {
                throw new ParseException(Previous(), "Only expected withing class declaration.");
            }

            throw new ParseException(Peek(), "Expect expression.");
        }

        private Expr Conditional()
        {
            Expr expr = Assignment();

            if (Match(TokenType.QUESTION))
            {
                Expr thenBranch = Expression();
                Consume(TokenType.COLON,
                    "Expect ':' after then branch of conditional expression.");
                Expr elseBranch = Conditional();
                expr = new Expr.Conditional(expr, thenBranch, elseBranch);
            }

            return expr;
        }

        private void Synchronize()
        {
            Advance();

            while (!IsAtEnd())
            {
                if (Previous().TokenType == TokenType.END_STATEMENT) return;

                switch (Peek().TokenType)
                {
                    case TokenType.CLASS:
                case TokenType.FUNCTION:
                case TokenType.VAR:
                case TokenType.FOR:
                case TokenType.IF:
                case TokenType.WHILE:
                case TokenType.PRINT:
                case TokenType.RETURN:
                    return;
                }

                Advance();
            }
        }

        private Token Consume(TokenType type, string msg)
        {
            if (Check(type)) return Advance();

            throw new ParseException(Peek(), msg);
        }

        private bool Match(params TokenType[] list)
        {
            foreach (var item in list)
            {
                if (Check(item))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private bool CheckNext(params TokenType[] list)
        {
            if (IsAtEnd()) return false;
            foreach (var type in list)
            {
                if (_tokens[current + 1].TokenType == type)
                    return true;
            }
            return false;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().TokenType == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }

        private bool IsAtEnd()
        {
            return Peek().TokenType == TokenType.EOF;
        }

        private Token Peek()
        {
            return _tokens[current];
        }

        private Token Previous()
        {
            return _tokens[current - 1];
        }
    }
}
