using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public class Parser
    {
        private List<Token> _tokens;
        private int current = 0;
        private int _loopDepth;
        private Token _currentClassToken;
        private bool _skipTests = false;

        public Parser(bool skipTests = false)
        {
            _skipTests = skipTests;
        }

        public List<Stmt> Parse(List<Token> tokens)
        {
            _tokens = tokens;
            var statements = new List<Stmt>();
            while (!IsAtEnd())
            {
                statements.Add(Declaration());
            }
            return statements;
        }

        private Stmt Declaration()
        {
            if (Match(TokenType.CLASS)) return ClassDeclaration();
            if (Check(TokenType.FUNCTION) && CheckNext(TokenType.IDENTIFIER))
            {
                Consume(TokenType.FUNCTION, null);
                return Function(FunctionType.Function);
            }
            if (Match(TokenType.VAR)) return VarDeclaration();

            if (Match(TokenType.TEST)) return TestDeclaration();
            if (Match(TokenType.TESTCASE)) return TestCaseDeclaration();

            return Statement();
        }

        private Stmt TestCaseDeclaration()
        {
            var testName = Consume(TokenType.IDENTIFIER, "TestCases must have a name.");

            Expr.Grouping valueGrouping = null;
            if (Match(TokenType.OPEN_PAREN)) valueGrouping = GroupingExpression();

            Consume(TokenType.OPEN_BRACE, "Expect '{' before block of a test.");
            var statements = Block();

            return new Stmt.TestCase(testName, valueGrouping, new Stmt.Block(statements));
        }

        private Stmt TestDeclaration()
        {
            var testToken = Previous();
            var testName = testToken.Copy(TokenType.IDENTIFIER, testToken.ToString());
            if (Check(TokenType.IDENTIFIER))
            {
                testName = Consume(TokenType.IDENTIFIER, null);
            }

            Consume(TokenType.OPEN_BRACE, "Expect '{' before block of a test.");
            var statements = Block();

            if (_skipTests)
                return null;

            return new Stmt.Test(testToken, testName, new Stmt.Block(statements));
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
                superclass = new Expr.Variable(Previous());
            }

            Consume(TokenType.OPEN_BRACE, "Expect { befefore class body.");

            Stmt.Function init = null;
            var metaMethods = new List<Stmt.Function>();
            var fields = new List<Stmt.Var>();
            var metaFields = new List<Stmt.Var>();

            while (!Check(TokenType.CLOSE_BRACE) && !IsAtEnd())
            {
                var funcType = FunctionType.Method;
                var isClassMeta = Match(TokenType.CLASS);
                if (Match(TokenType.VAR))
                {
                    var varStatements = FlattenChainOfVars(VarDeclaration());
                    foreach (var varStatement in varStatements)
                    {
                        (isClassMeta ? metaFields : fields).Add(varStatement);
                    }
                }
                else
                {
                    var func = Function(funcType);
                    if (!isClassMeta && func.name.Lexeme == Class.InitalizerFunctionName)
                    {
                        //todo force ordering if super also has an init, so if super has init(self, a,b), child must start with that

                        if (init != null)
                            throw new ClassException(func.name, $"Classes cannot have more than 1 init function.");

                        init = func;

                        if (init.function.parameters.Count == 0)
                        {
                            throw new ClassException(func.name,
                                $"Class init expects {Class.InitalizerParamZeroName} as argument zero.");
                        }
                        else if (init.function.parameters[0].Lexeme != Class.InitalizerParamZeroName)
                        {
                            throw new ClassException(func.name,
                                $"Class init argument zero found {init.function.parameters[0].Lexeme}" +
                                $", expected {Class.InitalizerParamZeroName}.");
                        }
                    }
                    else
                    {
                        metaMethods.Add(func);
                    }
                }
            }

            //validate duplicate methods
            foreach (var method in metaMethods)
            {
                if (metaMethods.Count(x => x.name.Lexeme == method.name.Lexeme) > 1)
                    throw new ClassException(method.name,
                        $"Classes cannot have Functions of identical names. Found more than 1 {method.name.Lexeme} in class {className.Lexeme}.");

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

            if (init == null)
            {
                //generate an empty init
                init = new Stmt.Function(className.Copy(TokenType.IDENTIFIER, Class.InitalizerFunctionName),
                    Class.EmptyInitFuncExpr());
            }

            return new Stmt.Class(
                className,
                superclass,
                init,
                metaMethods,
                fields,
                metaFields);
        }

        private List<Stmt.Var> FlattenChainOfVars(Stmt stmt)
        {
            var res = new List<Stmt.Var>();

            FlattenChainOfVars(stmt, res);

            return res;
        }

        private void FlattenChainOfVars(Stmt stmt, List<Stmt.Var> res)
        {
            if (stmt is Stmt.Var isVar)
            {
                res.Add(isVar);
            }
            else if (stmt is Stmt.Chain isChain)
            {
                FlattenChainOfVars(isChain.left, res);
                FlattenChainOfVars(isChain.right, res);
            }
        }

        private Stmt.Function Function(FunctionType functionType)
        {
            var name = Consume(TokenType.IDENTIFIER, $"Expect {functionType} name.");
            return new Stmt.Function(name, FunctionBody(functionType));
        }

        private Expr.Function FunctionBody(FunctionType functionType)
        {
            var prev = Previous();
            List<Token> parameters = new List<Token>();

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

            if (functionType == FunctionType.Init)
            {
                if (parameters.Count == 0 || parameters[0].Lexeme != Class.InitalizerParamZeroName)
                    throw new ParseException(prev, $"Expect {Class.InitalizerParamZeroName} as first param to {Class.InitalizerFunctionName}");
            }

            Consume(TokenType.OPEN_BRACE, $"Expect '{{' before {functionType} body.");
            var body = Block();
            return new Expr.Function(parameters, body);
        }

        private Stmt VarDeclaration()
        {
            if (Match(TokenType.OPEN_PAREN))
            {
                return MultiVarDeclaration();
            }
            Token name = Consume(TokenType.IDENTIFIER, "Expect variable name.");

            Expr initializer = null;
            if (Match(TokenType.ASSIGN))
            {
                initializer = Expression();
            }

            if (Match(TokenType.COMMA))
            {
                return new Stmt.Chain(
                    new Stmt.Var(name, initializer),
                    VarDeclaration());
            }
            else
            {
                Consume(TokenType.END_STATEMENT, "Expect end of statement after variable declaration.");
                return new Stmt.Var(name, initializer);
            }
        }

        private Stmt MultiVarDeclaration()
        {
            var nameList = new List<Token>();

            do
            {
                nameList.Add(Consume(TokenType.IDENTIFIER, "Expect variable name."));
            } while (Match(TokenType.COMMA));

            Consume(TokenType.CLOSE_PAREN, "Expect ')' after multivar name block.");

            Expr initializer = null;
            if (Match(TokenType.ASSIGN))
            {
                initializer = Expression();
            }

            Consume(TokenType.END_STATEMENT, "Expect end of statement after variable declaration.");
            return new Stmt.MultiVar(nameList, initializer);
        }

        private Stmt Statement()
        {
            if (Match(TokenType.LOOP)) return LoopStatement();
            if (Match(TokenType.FOR)) return ForStatement();
            if (Match(TokenType.IF)) return IfStatement();
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
            Expr.Grouping grouping = new Expr.Grouping(new List<Expr>() { });
            if (!Check(TokenType.END_STATEMENT))
            {
                if (Match(TokenType.OPEN_PAREN))
                    grouping = GroupingExpression();
                else
                    grouping = new Expr.Grouping(new List<Expr>() { Expression() });
            }
            Consume(TokenType.END_STATEMENT, "Expect ; after return value.");
            return new Stmt.Return(keyword, grouping);
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

        private Stmt ExpressionStatement()
        {
            var expr = Expression();
            Consume(TokenType.END_STATEMENT, "Expect end of statement after value.");
            return new Stmt.Expression(expr);
        }

        private Expr Expression() => Conditional();

        public void Reset()
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

                Token name;
                Expr obj;

                if (expr is Expr.Get exprGet)
                {
                    name = exprGet.name;
                    obj = exprGet.targetObj;

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
                else if (expr is Expr.Variable exprVar)
                {
                    name = exprVar.name;

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
                else if (expr is Expr.Grouping grouping)
                {
                    switch (equals.TokenType)
                    {
                        case TokenType.ASSIGN: return new Expr.Set(grouping, equals, value);
                    }
                    return expr;
                }
                else
                {
                    throw new ParseException(equals, "Invalid assignment target.");
                }
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
            var group = GroupingExpression();
            return new Expr.Call(callee, Previous(), group);
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

            if (Match(TokenType.IDENTIFIER))
            {
                return new Expr.Variable(Previous());
            }

            if (Match(TokenType.OPEN_PAREN)) return GroupingExpression();

            if (Match(TokenType.THROW)) return new Expr.Throw(Previous(), Check(TokenType.END_STATEMENT) ? null : Expression());

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
                Expr elseBranch = Expression();
                expr = new Expr.Conditional(expr, thenBranch, elseBranch);
            }

            return expr;
        }

        private Expr.Grouping GroupingExpression()
        {
            //handle single return
            if (Previous().TokenType != TokenType.OPEN_PAREN)
                return new Expr.Grouping(new List<Expr>() { Expression() });

            var list = new List<Expr>();

            //handle empty grouping e.g func call
            if (!Match(TokenType.CLOSE_PAREN))
            {
                //handle lists of expressions, this can then also be used for multi returns
                do
                {
                    list.Add(Expression());
                } while (Match(TokenType.COMMA));

                Consume(TokenType.CLOSE_PAREN, "Expect ')' after grouping.");
            }
            return new Expr.Grouping(list);
        }

        //private void Synchronize()
        //{
        //    Advance();

        //    while (!IsAtEnd())
        //    {
        //        if (Previous().TokenType == TokenType.END_STATEMENT) return;

        //        switch (Peek().TokenType)
        //        {
        //        case TokenType.CLASS:
        //        case TokenType.FUNCTION:
        //        case TokenType.VAR:
        //        case TokenType.FOR:
        //        case TokenType.IF:
        //        case TokenType.WHILE:
        //        case TokenType.RETURN:
        //            return;
        //        }

        //        Advance();
        //    }
        //}

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
