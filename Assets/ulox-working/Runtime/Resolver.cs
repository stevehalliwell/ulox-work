using System;
using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    //todo challenges
    public class Resolver : Expr.Visitor<Object>,
                               Stmt.Visitor
    {
        public class ResolverException : TokenException 
        {
            public ResolverException(Token token, string msg) : base(token, msg) { }
        }

        private List<Dictionary<string, bool>> scopes = new List<Dictionary<string, bool>>();
        private Interpreter _interpreter; 
        private FunctionType currentFunction = FunctionType.NONE;

        public enum FunctionType
        {
            NONE,
            FUNCTION,
            METHOD,
        }

        public Resolver(Interpreter interpreter)
        {
            _interpreter = interpreter;
        }

        public void Reset()
        {
            scopes.Clear();
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
            ResolveLocal(expr, expr.name);
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
                scopes.Last().TryGetValue(expr.name.Lexeme, out bool existingFlag) && existingFlag == false)
            {
                throw new ResolverException(expr.name, "Can't read local variable in its own initializer.");
            }
            
            ResolveLocal(expr, expr.name);
            return null;
        }

        private void ResolveLocal(Expr expr, Token name)
        {
            for (int i = scopes.Count-1; i >= 0 ; i--)
            {
                if(scopes[i].ContainsKey(name.Lexeme))
                {
                    _interpreter.Resolve(expr, scopes.Count - 1 - i);
                }
            }
        }

        public void Visit(Stmt.Block stmt)
        {
            BeginScope();
            Resolve(stmt.statements);
            EndScope();
        }

        private void BeginScope()
        {
            scopes.Add(new Dictionary<string, bool>());
        }

        private void EndScope()
        {
            scopes.RemoveAt(scopes.Count-1);
        }

        public void Visit(Stmt.Expression stmt)
        {
            Resolve(stmt.expression);
        }

        public void Visit(Stmt.Function stmt)
        {
            Declare(stmt.name);
            Define(stmt.name);

            ResolveFunction(stmt, FunctionType.FUNCTION);
        }

        private void ResolveFunction(Stmt.Function stmt, FunctionType functionType)
        {
            var enclosingFunctionType = currentFunction;
            currentFunction = functionType;

            BeginScope();
            foreach (var param in stmt.parameters)
            {
                Declare(param);
                Define(param);
            }
            Resolve(stmt.body);
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
            if(currentFunction == FunctionType.NONE)
            {
                throw new ResolverException(stmt.keyword, "Cannot return outside of a function.");
            }
            if(stmt.value != null)Resolve(stmt.value);
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

        private void Declare(Token name)
        {
            if (scopes.Count == 0) return;

            var scope = scopes.Last();
            if (scope.ContainsKey(name.Lexeme))
            {
                throw new ResolverException(name, "Already a variable with this name in this scope.");
            }

            scope.Add(name.Lexeme, false);
        }

        private void Define(Token name)
        {
            if (scopes.Count == 0) return;
            scopes.Last()[name.Lexeme] = true;
        }

        public void Visit(Stmt.While stmt)
        {
            Resolve(stmt.condition);
            Resolve(stmt.body);
        }

        public void Visit(Stmt.Class stmt)
        {
            Declare(stmt.name);

            BeginScope();
            scopes.Last()["this"] = true;

            foreach (Stmt.Function method in stmt.methods)
            {
                FunctionType declaration = FunctionType.METHOD;
                ResolveFunction(method, declaration);
            }

            EndScope();

            Define(stmt.name);
        }

        public object Visit(Expr.Get expr)
        {
            Resolve(expr.obj);
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
            ResolveLocal(expr, expr.keyword);
            return null;
        }
    }
}