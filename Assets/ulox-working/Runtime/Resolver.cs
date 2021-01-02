using System;
using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    //TODO the interpreter resolves at runtime but the resolver should calc
    //  the depth and slots in advance so single use environments are also speedy
    public class Resolver : Expr.Visitor<Object>,
                                Stmt.Visitor
    {
        private class VariableUse
        {
            public Token name;

            public enum State { Declared, Defined, Read };

            public State state;
            public int index;

            public VariableUse(Token Name, State _state, int _index)
            {
                name = Name; 
                state = _state;
                index = _index;
            }
        }

        private List<Dictionary<string, VariableUse>> scopes = new List<Dictionary<string, VariableUse>>();
        private Interpreter _interpreter;
        private FunctionType currentFunction = FunctionType.NONE;
        private ClassType currentClass = ClassType.NONE;
        private List<ResolverWarning> resolverWarnings = new List<ResolverWarning>();

        public List<ResolverWarning> ResolverWarnings => resolverWarnings;

        private enum FunctionType
        {
            NONE,
            FUNCTION,
            INITIALIZER,
            METHOD,
        }

        private enum ClassType
        {
            NONE,
            CLASS,
            SUBCLASS,
        }

        public Resolver(Interpreter interpreter)
        {
            _interpreter = interpreter;
        }

        public void Reset()
        {
            scopes.Clear();
            currentFunction = FunctionType.NONE;
            currentClass = ClassType.NONE;
            resolverWarnings = new List<ResolverWarning>();
        }

        public void Resolve(List<Stmt> statements)
        {
            foreach (var item in statements)
            {
                Resolve(item);
            }
        }

        public void Resolve(Stmt stmt)
        {
            stmt.Accept(this);
        }

        public void Resolve(Expr expr)
        {
            expr.Accept(this);
        }

        public object Visit(Expr.Assign expr)
        {
            Resolve(expr.value);
            ResolveLocal(expr, expr.name, false);
            return null;
        }

        public object Visit(Expr.Binary expr)
        {
            Resolve(expr.left);
            Resolve(expr.right);
            return null;
        }

        public object Visit(Expr.Call expr)
        {
            Resolve(expr.callee);
            foreach (var item in expr.arguments)
            {
                Resolve(item);
            }

            return null;
        }

        public object Visit(Expr.Grouping expr)
        {
            Resolve(expr.expression);
            return null;
        }

        public object Visit(Expr.Literal expr)
        {
            return null;
        }

        public object Visit(Expr.Logical expr)
        {
            Resolve(expr.left);
            Resolve(expr.right);
            return null;
        }

        public object Visit(Expr.Unary expr)
        {
            Resolve(expr.right);
            return null;
        }

        public object Visit(Expr.Variable expr)
        {
            if (scopes.Count > 0 &&
                scopes.Last().TryGetValue(expr.name.Lexeme, out var existingFlag) &&
                existingFlag.state == VariableUse.State.Declared)
            {
                throw new ResolverException(expr.name, "Can't read local variable in its own initializer.");
            }

            ResolveLocal(expr, expr.name, true);
            return null;
        }

        public void Visit(Stmt.Block stmt)
        {
            BeginScope();
            Resolve(stmt.statements);
            EndScope();
        }

        //todo take reference for index and distance
        private void ResolveLocal(Expr expr, Token name, bool isRead)
        {
            for (int i = scopes.Count - 1; i >= 0; i--)
            {
                if (scopes[i].ContainsKey(name.Lexeme))
                {
                    if (isRead)
                    {
                        scopes[i][name.Lexeme].state = VariableUse.State.Read;
                    }

                    return;
                }
            }
        }

        private void BeginScope()
        {
            scopes.Add(new Dictionary<string, VariableUse>());
        }

        private void Declare(Token name)
        {
            if (scopes.Count == 0) return;

            var scope = scopes.Last();
            if (scope.ContainsKey(name.Lexeme))
            {
                throw new ResolverException(name, "Already a variable with this name in this scope.");
            }

            scope.Add(name.Lexeme, new VariableUse(name, VariableUse.State.Declared, scope.Count));
        }

        private void Define(Token name)
        {
            if (scopes.Count == 0) return;
            var count = scopes.Last().Count;
            scopes.Last()[name.Lexeme].state = VariableUse.State.Defined;
        }

        private void EndScope()
        {
            foreach (var item in scopes.Last().Values)
            {
                if (item.state != VariableUse.State.Read)
                {
                    Warning(item.name, "Local variable is never read.");
                }
            }

            scopes.RemoveAt(scopes.Count - 1);
        }

        private void Warning(Token name, string msg)
        {
            resolverWarnings.Add(new ResolverWarning() { Token = name, Message = msg });
        }

        public void Visit(Stmt.Expression stmt)
        {
            Resolve(stmt.expression);
        }

        public void Visit(Stmt.Function stmt)
        {
            Declare(stmt.name);
            Define(stmt.name);

            ResolveFunction(stmt.function, FunctionType.FUNCTION);
        }

        public object Visit(Expr.Function expr)
        {
            ResolveFunction(expr, FunctionType.FUNCTION);
            return null;
        }

        private void ResolveFunction(Expr.Function func, FunctionType functionType)
        {
            var enclosingFunctionType = currentFunction;
            currentFunction = functionType;

            BeginScope();
            if (func.parameters != null)
            {
                foreach (var param in func.parameters)
                {
                    Declare(param);
                    Define(param);
                }
            }
            Resolve(func.body);
            EndScope();

            currentFunction = enclosingFunctionType;
        }

        public void Visit(Stmt.If stmt)
        {
            Resolve(stmt.condition);
            Resolve(stmt.thenBranch);
            if (stmt.elseBranch != null) Resolve(stmt.elseBranch);
        }

        public void Visit(Stmt.Print stmt)
        {
            Resolve(stmt.expression);
        }

        public void Visit(Stmt.Return stmt)
        {
            if (currentFunction == FunctionType.NONE)
            {
                throw new ResolverException(stmt.keyword, "Cannot return outside of a function.");
            }
            if (stmt.value != null)
            {
                if (currentFunction == FunctionType.INITIALIZER)
                    throw new ResolverException(stmt.keyword, "Cannot return a value from an initializer");

                Resolve(stmt.value);
            }
        }

        public void Visit(Stmt.Var stmt)
        {
            Declare(stmt.name);
            if (stmt.initializer != null)
            {
                Resolve(stmt.initializer);
            }
            Define(stmt.name);
        }

        public void Visit(Stmt.While stmt)
        {
            Resolve(stmt.condition);
            Resolve(stmt.body);
            if (stmt.increment != null) Resolve(stmt.increment);
        }

        public void Visit(Stmt.Class stmt)
        {
            var enclosingClass = currentClass;
            currentClass = ClassType.CLASS;

            Declare(stmt.name);
            Define(stmt.name);

            if (stmt.superclass != null &&
                stmt.name.Lexeme == stmt.superclass.name.Lexeme)
            {
                throw new ResolverException(stmt.superclass.name,
                    "A class can't inherit from itself.");
            }

            if (stmt.superclass != null)
            {
                currentClass = ClassType.SUBCLASS;
                Resolve(stmt.superclass);
            }

            foreach (var item in stmt.metaFields)
            {
                Resolve(item);
            }

            foreach (Stmt.Function method in stmt.metaMethods)
            {
                BeginScope();
                scopes.Last()["this"] = new VariableUse(stmt.name, VariableUse.State.Read, scopes.Last().Count);
                ResolveFunction(method.function, FunctionType.METHOD);
                EndScope();
            }

            if (stmt.superclass != null)
            {
                BeginScope();
                scopes.Last()["super"] = new VariableUse(stmt.name, VariableUse.State.Read, scopes.Last().Count);
            }

            BeginScope();
            scopes.Last()["this"] = new VariableUse(stmt.name, VariableUse.State.Read, scopes.Last().Count);

            foreach (var item in stmt.fields)
            {
                Resolve(item);
            }

            foreach (Stmt.Function method in stmt.methods)
            {
                FunctionType declaration = FunctionType.METHOD;
                if (method.name.Lexeme == "init")
                {
                    declaration = FunctionType.INITIALIZER;
                }
                ResolveFunction(method.function, declaration);
            }

            EndScope();

            if (stmt.superclass != null) EndScope();

            currentClass = enclosingClass;
        }

        public object Visit(Expr.Get expr)
        {
            Resolve(expr.obj);
            ResolveLocal(expr, expr.name, true);
            return null;
        }

        public object Visit(Expr.Set expr)
        {
            Resolve(expr.val);
            Resolve(expr.obj);
            return null;
        }

        public object Visit(Expr.This expr)
        {
            if (currentClass == ClassType.NONE)
                throw new ResolverException(expr.keyword, "Cannot use 'this' outside of a class.");

            ResolveLocal(expr, expr.keyword, false);
            return null;
        }

        public object Visit(Expr.Super expr)
        {
            if (currentClass == ClassType.NONE)
                throw new ResolverException(expr.keyword, "Cannot use 'super' outside of a class.");
            if (currentClass == ClassType.CLASS)
                throw new ResolverException(expr.keyword, "Cannot use 'super' in a class with no superclass.");

            ResolveLocal(expr, expr.keyword, false);
            return null;
        }

        public object Visit(Expr.Conditional expr)
        {
            Resolve(expr.condition);
            Resolve(expr.ifTrue);
            Resolve(expr.ifFalse);
            return null;
        }

        public void Visit(Stmt.Break stmt)
        {
        }

        public void Visit(Stmt.Continue stmt)
        {
        }

        public void Visit(Stmt.Chain stmt)
        {
            Resolve(stmt.left);
            Resolve(stmt.right);
        }
    }
}
