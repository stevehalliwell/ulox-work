using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public class Parser
    {
        private enum FunctionType { None, Function, Method, Get, Set,}
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
            var name = Consume(TokenType.IDENTIFIER, "Expect class name.");

            var previousClassToken = _currentClassToken;
            _currentClassToken = name;

            Expr.Variable superclass = null;
            if (Match(TokenType.LESS))
            {
                Consume(TokenType.IDENTIFIER, "Expect superclass name.");
                superclass = new Expr.Variable(Previous());
            }

            Consume(TokenType.OPEN_BRACE, "Expect { befefore class body.");

            var methods = new List<Stmt.Function>();
            var metaMethods = new List<Stmt.Function>();
            //todo condense field and metafield, so most detailed remains.
            //todo support get sets with initialisers
            var fields = new List<Stmt.Var>();
            var metaFields = new List<Stmt.Var>();
            while (!Check(TokenType.CLOSE_BRACE) && !IsAtEnd())
            {
                var funcType = FunctionType.Method;
                var isClassMethod = Match(TokenType.CLASS);
                Stmt.Function genFunc = null;
                if (Match(TokenType.GET))
                {
                    funcType = FunctionType.Get;
                    //check if its short hand and generate, if not then pass to function
                    if (CheckNext(TokenType.END_STATEMENT))
                    {
                        var getName = Consume(TokenType.IDENTIFIER, $"Expect field name after short hand 'get'.");
                        var getFuncName = getName.Copy(TokenType.IDENTIFIER, "Get" + getName.Lexeme);
                        genFunc = CreateGetMethod(name, getName, getFuncName);
                        Consume(TokenType.END_STATEMENT, "Expect ';' after short hand 'get'.");
                        (isClassMethod ? metaMethods : methods).Add(genFunc);
                        fields.Add(new Stmt.Var( getName, null));
                    }

                    if (genFunc == null)
                    {
                        genFunc = Function(funcType);
                        (isClassMethod ? metaMethods : methods).Add(genFunc);
                    }
                }
                else if (Match(TokenType.SET))
                {
                    funcType = FunctionType.Set;
                    if (CheckNext(TokenType.END_STATEMENT))
                    {
                        var setName = Consume(TokenType.IDENTIFIER, $"Expect field name after short hand 'set'.");
                        var setFuncName = setName.Copy(TokenType.IDENTIFIER, "Set" + setName.Lexeme);
                        var valueName = setName.Copy(TokenType.IDENTIFIER, "value");
                        genFunc = CreateSetMethod(name, setName, setFuncName, valueName);
                        Consume(TokenType.END_STATEMENT, "Expect ';' after short hand 'set'.");
                        (isClassMethod ? metaMethods : methods).Add(genFunc);
                        fields.Add(new Stmt.Var(setName, null));
                    }

                    if (genFunc == null)
                    {
                        genFunc = Function(funcType);
                        (isClassMethod ? metaMethods : methods).Add(genFunc);
                    }
                }
                else if (Match(TokenType.GETSET))
                {
                    //only allows short hand
                    var propName = Consume(TokenType.IDENTIFIER, $"Expect field name after short hand 'getset'.");
                    var getFuncName = propName.Copy(TokenType.IDENTIFIER, "Get" + propName.Lexeme);
                    var setFuncName = propName.Copy(TokenType.IDENTIFIER, "Set" + propName.Lexeme);
                    var valueName = propName.Copy(TokenType.IDENTIFIER, "value");
                    var genGetFunc = CreateGetMethod(name, propName, getFuncName);
                    var genSetFunc = CreateSetMethod(name, propName, setFuncName, valueName);
                    Consume(TokenType.END_STATEMENT, "Expect ';' after short hand 'get'.");
                    (isClassMethod ? metaMethods : methods).Add(genGetFunc);
                    (isClassMethod ? metaMethods : methods).Add(genSetFunc);

                    fields.Add(new Stmt.Var( propName, null));
                }
                else if (Match(TokenType.VAR))
                {
                    var varStatement = (Stmt.Var)VarDeclaration();
                    (isClassMethod? metaFields : fields).Add(varStatement);
                }
                else
                {
                    (isClassMethod ? metaMethods : methods).Add(Function(funcType));
                    //throw new ParseException(Peek(), "Unexpected token in class declaration.");
                }
            }

            //validate duplicate methods
            foreach (var method in methods)
            {
                if (methods.Count(x => x.name.Lexeme == method.name.Lexeme) > 1)
                    throw new ClassException(method.name, 
                        $"Classes cannot have methods of identical names. Found more than 1 {method.name.Lexeme} in class {name.Lexeme}.");
            }

            foreach (var method in metaMethods)
            {
                if (metaMethods.Count(x => x.name.Lexeme == method.name.Lexeme) > 1)
                    throw new ClassException(method.name,
                        $"Classes cannot have metaMethods of identical names. Found more than 1 {method.name.Lexeme} in class {name.Lexeme}.");
            }

            foreach (var field in fields)
            {
                if (fields.Count(x => x.name.Lexeme == field.name.Lexeme) > 1)
                    throw new ClassException(field.name,
                        $"Classes cannot have fields of identical names. Found more than 1 {field.name.Lexeme} in class {name.Lexeme}.");
            }

            foreach (var field in metaFields)
            {
                if (metaFields.Count(x => x.name.Lexeme == field.name.Lexeme) > 1)
                    throw new ClassException(field.name,
                        $"Classes cannot have metaFields of identical names. Found more than 1 {field.name.Lexeme} in class {name.Lexeme}.");
            }



            //TODO hold onto all get set var names add to an init method or __init or class ctor equiv
            //  without it if user doesn't manually create in init and calls get it's undefinied var

            Consume(TokenType.CLOSE_BRACE, "Expect } after class body.");

            _currentClassToken = previousClassToken;

            return new Stmt.Class(
                name, 
                superclass, 
                methods, 
                metaMethods,
                fields,
                metaFields);
        }

        private static Stmt.Function CreateSetMethod(Token name, Token setName, Token setFuncName, Token valueName)
        {
            return new Stmt.Function(setFuncName,
                new Expr.Function(new List<Token>() { valueName },
                    new List<Stmt>() {
                        new Stmt.Expression(new Expr.Set(
                            new Expr.This(name.Copy(TokenType.THIS, "this")),
                            setName,
                            new Expr.Variable(valueName)))}));
        }

        private static Stmt.Function CreateGetMethod(Token name, Token getName, Token getFuncName)
        {
            return new Stmt.Function(getFuncName,
                new Expr.Function(null,
                    new List<Stmt>() {
                        new Stmt.Return(name.Copy(TokenType.RETURN), new Expr.Get(
                            new Expr.This(name.Copy(TokenType.THIS, "this")), getName))}));
        }

        private Stmt.Function Function(FunctionType functionType)
        {
            var name = Consume(TokenType.IDENTIFIER, $"Expect {functionType} name.");
            return new Stmt.Function(name, FunctionBody(functionType));
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

            if(functionType == FunctionType.Get && parameters?.Count != 0)
            {
                throw new ClassException(_currentClassToken, "Cannot have arguments to a Get.");
            }

            if (functionType == FunctionType.Set && parameters?.Count != 0)
            {
                throw new ClassException(_currentClassToken, "Cannot have arguments to a Set. A value variable so auto generated");
            }

            if (functionType == FunctionType.Set)
            {
                var createdValueParam = Previous().Copy(TokenType.IDENTIFIER, "value");
                parameters.Add(createdValueParam);
            }

            Consume(TokenType.OPEN_BRACE, $"Expect '{{' before {functionType} body.");
            var body = Block();
            return new Expr.Function(parameters, body);
        }

        private Stmt VarDeclaration()
        {
            Token name = Consume(TokenType.IDENTIFIER, "Expect variable name.");

            Expr initializer = null;
            if (Match(TokenType.ASSIGN))
            {
                initializer = Expression();
            }

            Consume(TokenType.END_STATEMENT, "Expect end of statement after variable declaration.");
            return new Stmt.Var(name, initializer);
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
                        case TokenType.MINUS_EQUAL: return new Expr.Assign(name, new Expr.Binary(expr, equals.Copy(TokenType.MINUS), value));
                        case TokenType.PLUS_EQUAL: return new Expr.Assign(name, new Expr.Binary(expr, equals.Copy(TokenType.PLUS), value));
                        case TokenType.STAR_EQUAL: return new Expr.Assign(name, new Expr.Binary(expr, equals.Copy(TokenType.STAR), value));
                        case TokenType.SLASH_EQUAL: return new Expr.Assign(name, new Expr.Binary(expr, equals.Copy(TokenType.SLASH), value));
                        case TokenType.PERCENT_EQUAL: return new Expr.Assign(name, new Expr.Binary(expr, equals.Copy(TokenType.PERCENT), value));
                        case TokenType.ASSIGN: return new Expr.Assign(name, value);
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
                    expr = new Expr.Get(expr, name);
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
                Consume(TokenType.DOT, "Expect '.' after 'super'.");
                Token method = Consume(TokenType.IDENTIFIER,
                    "Expect superclass method name.");
                return new Expr.Super(keyword, method);
            }

            if (Match(TokenType.THIS)) return new Expr.This(Previous());

            if (Match(TokenType.IDENTIFIER))
            {
                return new Expr.Variable(Previous());
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

        private bool CheckNext(TokenType type)
        {
            if (IsAtEnd()) return false;
            return _tokens[current + 1].TokenType == type;
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