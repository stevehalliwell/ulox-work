using System;
using System.Collections.Generic;

namespace ULox
{
    //TODO see challenges
    public class Interpreter : Expr.Visitor<Object>,
                               Stmt.Visitor
    {
        public class RuntimeTypeException : TokenException
        {
            public RuntimeTypeException(Token token, string msg)
                : base(token, msg)
            { }
        }

        private Action<string> _logger;
        private Environment environment = new Environment();

        public Interpreter(Action<string> logger)
        {
            _logger = logger;
        }

        public void Interpret(List<Stmt> statements)
        {
            try
            {
                foreach (var item in statements)
                {
                    Execute(item);
                }
            }
            catch (RuntimeTypeException error)
            {
                _logger?.Invoke(error.Message);
            }
        }

        private void Execute(Stmt stmt) => stmt.Accept(this);

        public object Visit(Expr.Binary expr)
        {
            object left = Evaluate(expr.left);
            object right = Evaluate(expr.right);

            switch (expr.op.TokenType)
            {
            case TokenType.BANG_EQUAL:
                return !IsEqual(left, right);
            case TokenType.EQUALITY:
                return IsEqual(left, right);

            case TokenType.GREATER:
                CheckNumberOperands(expr.op, left, right);
                return (double)left > (double)right;
            case TokenType.GREATER_EQUAL:
                CheckNumberOperands(expr.op, left, right);
                return (double)left >= (double)right;
            case TokenType.LESS:
                CheckNumberOperands(expr.op, left, right);
                return (double)left < (double)right;
            case TokenType.LESS_EQUAL:
                return (double)left <= (double)right;

            case TokenType.MINUS:
                CheckNumberOperands(expr.op, left, right);
                return (double)left - (double)right;
            case TokenType.SLASH:
                CheckNumberOperands(expr.op, left, right);
                return (double)left / (double)right;
            case TokenType.STAR:
                CheckNumberOperands(expr.op, left, right);
                return (double)left * (double)right;
            case TokenType.PLUS:
                {
                    if (left is double leftD && right is double rightD)
                        return leftD + rightD;
                    if (left is string leftS && right is string rightS)
                        return leftS + rightS;

                    throw new RuntimeTypeException(expr.op, "Operands must be numbers or strings.");
                }
                break;
            }

            return null;
        }

        public object Visit(Expr.Grouping expr) => Evaluate(expr.expression);

        public object Visit(Expr.Literal expr) => expr.value;

        public object Visit(Expr.Unary expr)
        {
            var right = Evaluate(expr.right);

            switch(expr.op.TokenType)
            {
            case TokenType.MINUS:
                CheckNumberOperand(expr.op, right);
                return -(double)right;
            case TokenType.BANG:
                CheckNumberOperand(expr.op, right);
                return !IsTruthy(right);
            }

            return null;
        }

        private bool IsTruthy(object obj)
        {
            if (obj == null) return false;
            if (obj is bool objBool) return objBool;
            return true;
        }

        private object Evaluate(Expr expression) => expression.Accept(this);

        private static bool IsEqual(object left, object right)
        {
            if (left == null && right == null) return true;
            if (left == null) return false;
            return left.Equals(right);
        }

        private static void CheckNumberOperands(Token op, object left, object right)
        {
            if (left is double && right is double) return;
            throw new RuntimeTypeException(op, "Operands must be numbers.");
        }

        private static void CheckNumberOperand(Token op,object right)
        {
            if (right is double) return;
            throw new RuntimeTypeException(op, "Operands must be numbers.");
        }

        private static string Stringify(object value)
        {
            if (value == null) return "null";
            return value.ToString();
        }

        public void Visit(Stmt.Expression stmt) => Evaluate(stmt.expression);

        public void Visit(Stmt.Print stmt) => _logger?.Invoke(Stringify(Evaluate(stmt.expression)));

        public void Visit(Stmt.Var stmt)
        {
            Object value = null;
            if (stmt.initializer != null)
            {
                value = Evaluate(stmt.initializer);
            }

            environment.Define(stmt.name.Lexeme, value);
        }

        public object Visit(Expr.Variable expr) => environment.Get(expr.name);

        public object Visit(Expr.Assign expr)
        {
            var val = Evaluate(expr);
            environment.Assign(expr.name, val);
            return val;
        }

        public void Visit(Stmt.Block stmt) => ExecuteBlock(stmt.statements, new Environment(environment));

        private void ExecuteBlock(List<Stmt> statements, Environment environment)
        {
            var prevEnv = this.environment;
            try
            {
                this.environment = environment;
                foreach (var stmt in statements)
                {
                    Execute(stmt);
                }
            }
            finally
            {
                this.environment = prevEnv;
            }
        }
    }
}