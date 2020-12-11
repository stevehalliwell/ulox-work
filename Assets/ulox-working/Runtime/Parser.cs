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

        public List<Stmt> Parse(List<Token> tokens)
        {
            _tokens = tokens;
            var statements = new List<Stmt>();
            try
            {
                while(!IsAtEnd())
                {
                    statements.Add(Declaration());
                }

            }
            catch (ParseException exception)
            {
                return null;
            }
            return statements;
        }
        private Stmt Declaration()
        {
            try
            {
                if (Match(TokenType.VAR)) return VarDeclaration();

                return Statement();
            }
            catch (ParseException exception)
            {
                Synchronize();
                return null;
            }
        }

        private Stmt VarDeclaration()
        {
            Token name = Consume(TokenType.IDENT, "Expect variable name.");

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
            if (Match(TokenType.PRINT)) return PrintStatement();

            return ExpressionStatement();
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

        private Expr Expression() => Assignment();

        internal void Reset()
        {
            _tokens = null;
            current = 0;
        }

        private Expr Assignment()
        {
            Expr expr = Equality();

            if (Match(TokenType.ASSIGN))
            {
                Token equals = Previous();
                Expr value = Assignment();

                if (expr is Expr.Variable varExpr) {
                    Token name = varExpr.name;
                    return new Expr.Assign(name, value);
                }

                throw new ParseException(equals, "Invalid assignment target.");
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

            return Primary();
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

            if (Match(TokenType.IDENT))
            {
                return new Expr.Variable(Previous());
            }

            if (Match(TokenType.OPEN_PAREN))
            {
                Expr expr = Expression();
                Consume(TokenType.CLOSE_PAREN, "Expect ')' after expression.");
                return new Expr.Grouping(expr);
            }

            throw new ParseException(Peek(), "Expect expression.");
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