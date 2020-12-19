using System;
using System.Collections.Generic;

namespace ULox
{

    //TODO see challenges
    //todo add tests
    public class Parser
    {
        public class ParseException : TokenException
        {
            public ParseException(Token token, string msg)
                 : base(token, msg)
            { }
        }

        private List<Token> _tokens;
        private int current = 0;
        public bool CatchAndSynch { get; set; } = true;
        private int _loopDepth;

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
                if (Match(TokenType.FUNCTION)) return Function("function");
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

            Expr.Variable superclass = null;
            if (Match(TokenType.LESS))
            {
                Consume(TokenType.IDENTIFIER, "Expect superclass name.");
                superclass = new Expr.Variable(Previous());
            }

            Consume(TokenType.OPEN_BRACE, "Expect { befefore class body.");

            var methods = new List<Stmt.Function>();
            while (!Check(TokenType.CLOSE_BRACE) && !IsAtEnd())
            {
                methods.Add(Function("Method"));
            }

            Consume(TokenType.CLOSE_BRACE, "Expect } after class body.");

            return new Stmt.Class(name, superclass, methods);
        }

        private Stmt.Function Function(string kind)
        {
            var name = Consume(TokenType.IDENTIFIER, "Expect " + kind + " name.");

            Consume(TokenType.OPEN_PAREN, "Expect '(' after " + kind + " name.");
            var parameters = new List<Token>();
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

            Consume(TokenType.OPEN_BRACE, "Expect '{' before " + kind + " body.");
            var body = Block();
            return new Stmt.Function(name, parameters, body);
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

            if (Match(TokenType.ASSIGN))
            {
                Token equals = Previous();
                Expr value = Assignment();

                if (expr is Expr.Variable varExpr)
                {
                    Token name = varExpr.name;
                    return new Expr.Assign(name, value);
                }
                else if (expr is Expr.Get exprGet)
                {
                    return new Expr.Set(exprGet.obj, exprGet.name, value);
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

            while (Match(TokenType.MINUS, TokenType.PLUS))
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