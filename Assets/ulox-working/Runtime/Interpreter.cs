﻿using System;
using System.Collections.Generic;

namespace ULox
{
    public class Interpreter : Expr.Visitor<Object>,
                               Stmt.Visitor
    {
        public class InterpreterControlException : Exception
        {
            public Token From { get; set; }

            public InterpreterControlException(Token from)
            {
                From = from;
            }
        }

        public class Return : InterpreterControlException
        {
            public object Value { get; set; }

            public Return(Token from, object val) : base(from)
            {
                Value = val;
            }
        }

        public class Break : InterpreterControlException
        {
            public Break(Token from) : base(from)
            {
            }
        }

        public class Continue : InterpreterControlException
        {
            public Continue(Token from) : base(from)
            {
            }
        }

        private Environment _globals = new Environment(null);
        private IEnvironment _currentEnvironment;

        public Environment Globals => _globals;

        public IEnvironment CurrentEnvironment => _currentEnvironment;

        public Interpreter(bool useREPL = false)
        {
            _currentEnvironment = Globals;
        }

        public void Interpret(List<Stmt> statements)
        {
            foreach (var item in statements)
            {
                Execute(item);
            }
        }

        public void REPLInterpret(List<Stmt> statements, Action<string> output)
        {
            foreach (var item in statements)
            {
                if (item is Stmt.Expression stmtExpr)
                {
                    var res = Evaluate(stmtExpr.expression);
                    output?.Invoke(res?.ToString() ?? "null");
                }
                else
                {
                    Execute(item);
                }
            }
        }

        public void ExecuteBlock(List<Stmt> statements, Environment environment)
        {
            var prevEnv = this._currentEnvironment;
            try
            {
                this._currentEnvironment = environment;
                foreach (var stmt in statements)
                {
                    Execute(stmt);
                }
            }
            finally
            {
                this._currentEnvironment = prevEnv;
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

                case TokenType.PERCENT:
                    CheckNumberOperands(expr.op, left, right);
                    return (double)left % (double)right;

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

        public object Evaluate(Expr expression) => expression?.Accept(this) ?? null;

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

        public void Visit(Stmt.Expression stmt) => Evaluate(stmt.expression);

        public object Visit(Expr.Variable expr)
        {
            if (expr.varLoc == EnvironmentVariableLocation.Invalid)
            {
                try
                {
                    expr.varLoc = _currentEnvironment.FindLocation(expr.name.Lexeme);
                }
                catch (LoxException)
                {
                    throw new EnvironmentException(expr.name, $"Undefined variable {expr.name.Lexeme}");
                }
            }

            return _currentEnvironment.Ancestor(expr.varLoc.depth).FetchObject(expr.varLoc.slot);
        }

        public object Visit(Expr.Assign expr)
        {
            var val = Evaluate(expr.value);
            if (expr.varLoc == EnvironmentVariableLocation.Invalid)
            {
                try
                {
                    expr.varLoc = _currentEnvironment.FindLocation(expr.name.Lexeme);
                }
                catch (LoxException)
                {
                    throw new EnvironmentException(expr.name, $"Undefined variable {expr.name.Lexeme}");
                }
            }
            _currentEnvironment.Ancestor(expr.varLoc.depth).AssignSlot(expr.varLoc.slot, val);
            return val;
        }

        public void Visit(Stmt.Block stmt) => ExecuteBlock(stmt.statements, new Environment(_currentEnvironment));

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

                if (stmt.increment != null) Execute(stmt.increment);
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
            var args = new object[expr.arguments.Count];
            var i = 0;
            foreach (var item in expr.arguments)
            {
                args[i] = Evaluate(item);
                i++;
            }

            if (callee is ICallable calleeCallable)
            {
                if (args.Length != calleeCallable.Arity)
                    throw new RuntimeCallException(expr.paren,
                        $"Expected { calleeCallable.Arity} args but got { args.Length }");

                return calleeCallable.Call(this, args);
            }

            throw new RuntimeTypeException(expr.paren, "Can only call function types");
        }

        public object Visit(Expr.Function expr)
        {
            //todo if it doesn't use closure does it need current env?
            return new Function(null, expr, _currentEnvironment, false);
        }

        public void Visit(Stmt.Return stmt)
        {
            object val = null;
            if (stmt.value != null) val = Evaluate(stmt.value);

            throw new Return(stmt.keyword, val);
        }

        public void Visit(Stmt.Var stmt)
        {
            Object value = null;
            if (stmt.initializer != null)
            {
                value = Evaluate(stmt.initializer);
            }

            if (stmt.knownSlot != EnvironmentVariableLocation.InvalidSlot)
                _currentEnvironment.DefineSlot(stmt.name.Lexeme, stmt.knownSlot, value);
            else
                _currentEnvironment.DefineInAvailableSlot(stmt.name.Lexeme, value);
        }

        public void Visit(Stmt.Function stmt)
        {
            var func = new Function(stmt.name.Lexeme, stmt.function, _currentEnvironment, false);

            if (stmt.knownSlot != EnvironmentVariableLocation.InvalidSlot)
                _currentEnvironment.DefineSlot(stmt.name.Lexeme, stmt.knownSlot, func);
            else
                _currentEnvironment.DefineInAvailableSlot(stmt.name.Lexeme, func);
        }

        public void Visit(Stmt.Class stmt)
        {
            Class superclass = null;
            if (stmt.superclass != null)
            {
                superclass = Evaluate(stmt.superclass) as Class;
                if (superclass == null)
                {
                    throw new RuntimeTypeException(stmt.superclass.name,
                        "Superclass must be a class.");
                }
            }

            if (stmt.knownSlot != EnvironmentVariableLocation.InvalidSlot)
                _currentEnvironment.DefineSlot(stmt.name.Lexeme, stmt.knownSlot, null);
            else
                stmt.knownSlot = _currentEnvironment.DefineInAvailableSlot(stmt.name.Lexeme, null);

            if (stmt.superclass != null)
            {
                _currentEnvironment = new Environment(_currentEnvironment);
                _currentEnvironment.DefineSlot("super", Class.SuperSlot, superclass);
            }

            var classMethods = new Dictionary<string, Function>();
            foreach (var method in stmt.metaMethods)
            {
                var func = new Function(method.name.Lexeme, method.function, _currentEnvironment, false);
                classMethods[method.name.Lexeme] = func;
            }

            var metaClass = new Class(null, stmt.name.Lexeme + "_meta", null, classMethods, null, null);

            var methods = new Dictionary<string, Function>();
            foreach (Stmt.Function method in stmt.methods)
            {
                var function = new Function(
                    method.name.Lexeme,
                    method.function,
                    _currentEnvironment,
                    method.name.Lexeme == "init");

                methods[method.name.Lexeme] = function;
            }

            var @class = new Class(
                metaClass,
                stmt.name.Lexeme,
                superclass,
                methods,
                stmt.fields,
                _currentEnvironment);

            if (stmt.metaFields != null)
            {
                foreach (var item in stmt.metaFields)
                {
                    @class.Set(item.name.Lexeme, Evaluate(item.initializer));
                }
            }

            if (superclass != null)
            {
                _currentEnvironment = _currentEnvironment.Enclosing;
            }

            _currentEnvironment.AssignSlot(stmt.knownSlot, @class);
        }

        public object Visit(Expr.Get expr)
        {
            var obj = Evaluate(expr.obj);
            if (obj is Instance objInst)
            {
                object result = null;

                if (expr.varLoc != EnvironmentVariableLocation.Invalid)
                {
                    result = objInst.Ancestor(expr.varLoc.depth).FetchObject(expr.varLoc.slot);
                }
                else
                {
                    result = objInst.Get(expr.name);
                }

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

            if (obj == null)
            {
                throw new RuntimeTypeException(expr.name, "Only instances have fields.");
            }

            var val = Evaluate(expr.val);
            obj.Set(expr.name.Lexeme, val);
            return val;
        }

        public object Visit(Expr.This expr)
        {
            if (expr.varLoc == EnvironmentVariableLocation.Invalid)
            {
                try
                {
                    expr.varLoc = _currentEnvironment.FindLocation(expr.keyword.Lexeme);
                }
                catch (LoxException)
                {
                    throw new EnvironmentException(expr.keyword, $"Undefined variable {expr.keyword.Lexeme}");
                }
            }
            return _currentEnvironment.Ancestor(expr.varLoc.depth).FetchObject(expr.varLoc.slot);
        }

        //todo super and this behave very differently, can they be uniformed
        public object Visit(Expr.Super expr)
        {
            if (expr.superVarLoc == EnvironmentVariableLocation.Invalid)
                expr.superVarLoc = _currentEnvironment.FindLocation("super");

            var superclass = (Class)_currentEnvironment.Ancestor(expr.superVarLoc.depth).FetchObject(expr.superVarLoc.slot);

            if (!string.IsNullOrEmpty(expr.classNameToken.Lexeme))
            {
                while (superclass != null && superclass.Name != expr.classNameToken.Lexeme)
                {
                    superclass = superclass.Super;
                }

                if (superclass == null)
                {
                    throw new RuntimeTypeException(expr.classNameToken,
                        $"Could not find parent class of name '{expr.classNameToken.Lexeme}' via 'super'.");
                }
            }

            if (expr.thisVarLoc == EnvironmentVariableLocation.Invalid)
                expr.thisVarLoc = _currentEnvironment.FindLocation("this");

            var inst = (Instance)_currentEnvironment.Ancestor(expr.thisVarLoc.depth).FetchObject(expr.thisVarLoc.slot);

            var method = superclass.FindMethod(expr.method.Lexeme);

            if (method == null)
            {
                throw new RuntimeTypeException(expr.method,
                    $"Could not find '{expr.method.Lexeme}'via 'super'.");
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

        public static object SantizeObject(object o)
        {
            if (o is int || o is float || o is long)
                return Convert.ToDouble(o);
            return o;
        }

        public static void SantizeObjects(object[] objs)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                objs[i] = SantizeObject(objs[i]);
            }
        }

        public void Visit(Stmt.Chain stmt)
        {
            Execute(stmt.left);
            Execute(stmt.right);
        }
    }
}
