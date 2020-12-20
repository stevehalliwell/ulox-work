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

        public class RuntimeCallException : RuntimeTypeException
        {
            public RuntimeCallException(Token token, string msg) : base(token, msg) { }
        }

        public class InterpreterControlException : Exception 
        {
            public Token From { get; set; }
            public InterpreterControlException(Token from) { From = from; }
        }

        public class Return : InterpreterControlException
        {
            public object Value { get; set; }
            public Return(Token from, object val):base(from)
            {
                Value = val;
            }
        }

        public class Break : InterpreterControlException
        {
            public Break(Token from) : base(from) { }
        }

        public class Continue : InterpreterControlException
        {
            public Continue(Token from):base(from){}
        }

        private Action<string> _logger;
        private Environment _globals = new Environment();
        private Environment currentEnvironment;
        private Dictionary<Expr, int> localsSideTable = new Dictionary<Expr, int>();

        public Environment Globals => _globals;

        public Interpreter(Action<string> logger)
        {
            _logger = logger;
            currentEnvironment = Globals;

            Globals.Define("clock", new NativeExpression(() => System.DateTime.Now.Ticks));
        }

        public void Interpret(List<Stmt> statements)
        {
            foreach (var item in statements)
            {
                Execute(item);
            }
        }

        public void ExecuteBlock(List<Stmt> statements, Environment environment)
        {
            var prevEnv = this.currentEnvironment;
            try
            {
                this.currentEnvironment = environment;
                foreach (var stmt in statements)
                {
                    Execute(stmt);
                }
            }
            finally
            {
                this.currentEnvironment = prevEnv;
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
                    //we want to concat with + but the starting var may be empty or null
                    if (left is null)
                        return right;
                    else if (right is null)
                        return left;

                    if (left is double leftD && right is double rightD)
                        return leftD + rightD;
                    if (left is string leftS)
                    {
                        if (right is string rightS)
                            return leftS + rightS;
                        else 
                            return leftS + right.ToString();
                            
                    }
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

            switch (expr.op.TokenType)
            {
            case TokenType.MINUS:
                CheckNumberOperand(expr.op, right);
                return -(double)right;
            case TokenType.BANG:
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

        private static void CheckNumberOperand(Token op, object right)
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

            currentEnvironment.Define(stmt.name.Lexeme, value);
        }

        public object Visit(Expr.Variable expr) => LookUpVariable(expr.name, expr);

        private object LookUpVariable(Token name, Expr expr)
        {
            if(localsSideTable.TryGetValue(expr, out int distance))
            {
                return currentEnvironment.GetAt(distance, name);
            }

            return Globals.Get(name);
        }

        public object Visit(Expr.Assign expr)
        {
            var val = Evaluate(expr.value);

            if(localsSideTable.TryGetValue(expr, out int distance))
            {
                currentEnvironment.AssignAt(distance, expr.name, val);
            }
            else
            {
                Globals.Assign(expr.name, val);
            }

            return val;
        }

        public void Visit(Stmt.Block stmt) => ExecuteBlock(stmt.statements, new Environment(currentEnvironment));


        public void Visit(Stmt.If stmt)
        {
            if (IsTruthy(Evaluate(stmt.condition)))
                Execute(stmt.thenBranch);
            else if (stmt.elseBranch != null)
                Execute(stmt.elseBranch);
        }

        public object Visit(Expr.Logical expr)
        {
            var left = Evaluate(expr.left);

            if (expr.op.TokenType == TokenType.OR)
            {
                if (IsTruthy(left)) return left;
            }
            else
            {
                if (!IsTruthy(left)) return left;
            }

            return Evaluate(expr.right);
        }

        public void Visit(Stmt.While stmt)
        {
            while (IsTruthy(Evaluate(stmt.condition)))
            {
                try
                {
                    Execute(stmt.body);
                }
                catch (Break)
                {
                    return;
                }
                catch (Continue)
                { 
                }

                if(stmt.increment != null) Execute(stmt.increment);
            }
        }

        public void Visit(Stmt.Break stmt)
        {
            throw new Break(stmt.keyword);
        }

        public void Visit(Stmt.Continue stmt)
        {
            throw new Continue(stmt.keyword);
        }

        public object Visit(Expr.Call expr)
        {
            var callee = Evaluate(expr.callee);
            var args = new List<Object>();
            foreach (var item in expr.arguments)
            {
                args.Add(Evaluate(item));
            }

            if (callee is ICallable calleeCallable)
            {
                if (args.Count != calleeCallable.Arity)
                    throw new RuntimeCallException(expr.paren,
                        $"Expected { calleeCallable.Arity} args but got { args.Count }");

                return calleeCallable.Call(this, args);
            }

            throw new RuntimeTypeException(expr.paren, "Can only call function types");
        }

        public void Visit(Stmt.Function stmt)
        {
            var func = new Function(stmt.name.Lexeme, stmt.function, currentEnvironment, false);
            currentEnvironment.Define(stmt.name.Lexeme, func);
        }

        public object Visit(Expr.Function expr)
        {
            return new Function(null, expr, currentEnvironment, false);
        }

        public void Visit(Stmt.Return stmt)
        {
            object val = null;
            if (stmt.value != null) val = Evaluate(stmt.value);

            throw new Return(stmt.keyword, val);
        }

        public void Resolve(Expr expr, int depth)
        {
            localsSideTable.Add(expr, depth);
        }

        public void Visit(Stmt.Class stmt)
        {
            Class superclass = null;
            if (stmt.superclass != null)
            {
                superclass = Evaluate(stmt.superclass) as Class;
                if (superclass == null) {
                    throw new RuntimeTypeException(stmt.superclass.name,
                        "Superclass must be a class.");
                }
            }

            currentEnvironment.Define(stmt.name.Lexeme, null);

            if (stmt.superclass != null)
            {
                currentEnvironment = new Environment(currentEnvironment);
                currentEnvironment.Define("super", superclass);
            }


            var methods = new Dictionary<string, Function>();
            foreach (Stmt.Function method in stmt.methods)
            {
                var function = new Function(
                    method.name.Lexeme,
                    method.function, 
                    currentEnvironment,
                    method.name.Lexeme == "init");

                methods[method.name.Lexeme] = function;
            }

            var @class = new Class(stmt.name.Lexeme, superclass, methods);
            if (superclass != null)
            {
                currentEnvironment = currentEnvironment.Enclosing;
            }
            currentEnvironment.Assign(stmt.name, @class);
        }

        public object Visit(Expr.Get expr)
        {
            var obj = Evaluate(expr.obj);
            if (obj is Instance objInst)
            {
                object result = objInst.Get(expr.name);
                if (result is Function resultFunc)
                {
                    if (resultFunc.IsGetter)
                    {
                        result = resultFunc.Call(this, null);
                    }
                }
                return result;
            }

            throw new RuntimeTypeException(expr.name, "Only instances have properties.");
        }

        public object Visit(Expr.Set expr)
        {
            var obj = Evaluate(expr.obj) as Instance;

            if(obj == null)
            {
                throw new RuntimeTypeException(expr.name, "Only instances have fields.");
            }

            var val = Evaluate(expr.val);
            obj.Set(expr.name, val);
            return val;
        }

        public object Visit(Expr.This expr)
        {
            return LookUpVariable(expr.keyword, expr);
        }

        public object Visit(Expr.Super expr)
        {
            int distance = localsSideTable[expr];
            var superclass = (Class)currentEnvironment.GetAtDirect(distance, "super");

            var inst = (Instance)currentEnvironment.GetAtDirect(distance - 1, "this");

            var method = superclass.FindMethod(expr.method.Lexeme);

            if (method == null)
            {
                throw new RuntimeTypeException(expr.method,
                    "Undefined property '" + expr.method.Lexeme + "'.");
            }
            return method.Bind(inst);
        }

        public object Visit(Expr.Conditional expr)
        {
            if (IsTruthy(Evaluate(expr.condition)))
                return Evaluate(expr.ifTrue);
            else 
                return Evaluate(expr.ifFalse);
        }
    }
}