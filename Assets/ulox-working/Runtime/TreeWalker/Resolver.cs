using System.Collections.Generic;

namespace ULox
{
    public class Resolver : Expr.Visitor<object>,
                                Stmt.Visitor
    {
        private class VariableUse
        {
            public Token name;

            public enum State { Declared, Defined, Read };

            public State state;

            public VariableUse(Token Name, State _state)
            {
                name = Name;
                state = _state;
            }
        }

        private class ScopeInfo
        {
            public Dictionary<string, VariableUse> localVariables = new Dictionary<string, VariableUse>();
        }

        private Stack<ScopeInfo> _scopes = new Stack<ScopeInfo>();
        private FunctionType _currentFunctionType = FunctionType.None;
        private Expr.Function _currentExprFunc;
        private Stmt.Class _currentClass;
        private List<ResolverWarning> _resolverWarnings = new List<ResolverWarning>();

        public List<ResolverWarning> ResolverWarnings => _resolverWarnings;

        private enum ClassType
        {
            NONE,
            CLASS,
            SUBCLASS,
        }

        public Resolver()
        {
            Reset();
        }

        public void Reset()
        {
            _scopes.Clear();

            _currentFunctionType = FunctionType.None;
            _currentClass = null;
            _resolverWarnings = new List<ResolverWarning>();
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

        public object Visit(Expr.Binary expr)
        {
            Resolve(expr.left);
            Resolve(expr.right);
            return null;
        }

        public object Visit(Expr.Call expr)
        {
            Resolve(expr.callee);
            Resolve(expr.arguments);

            return null;
        }

        public object Visit(Expr.Grouping expr)
        {
            foreach (var item in expr.expressions)
            {
                Resolve(item);
            }
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

        public void Visit(Stmt.Block stmt)
        {
            Resolve(stmt.statements);
        }

        private void ResolveLocal(Token name, bool isRead)
        {
            if (_scopes.Count == 0) return;

            var scope = _scopes.Peek();

            if (scope.localVariables.TryGetValue(name.Lexeme, out var varUse))
            {
                if (isRead)
                {
                    varUse.state = VariableUse.State.Read;
                }
            }
        }

        private void BeginScope()
        {
            _scopes.Push(new ScopeInfo());
        }

        private void Declare(Token name)
        {
            if (_scopes.Count == 0) return;

            var scope = _scopes.Peek();
            if (scope.localVariables.ContainsKey(name.Lexeme))
            {
                throw new ResolverException(name, "Already a variable with this name in this scope.");
            }

            scope.localVariables.Add(name.Lexeme, new VariableUse(name, VariableUse.State.Declared));
        }

        private void Define(Token name)
        {
            if (_scopes.Count == 0) return;
            var scope = _scopes.Peek();
            var count = scope.localVariables.Count;
            scope.localVariables[name.Lexeme].state = VariableUse.State.Defined;
        }

        private void DeclareDefineRead(string name)
        {
            if (_scopes.Count == 0) return;

            var scope = _scopes.Peek();
            scope.localVariables.Add(name, new VariableUse(new Token(TokenType.IDENTIFIER, name, name, -1, -1), VariableUse.State.Read));
        }

        private void EndScope()
        {
            var scope = _scopes.Peek();
            foreach (var item in scope.localVariables.Values)
            {
                if (item.state != VariableUse.State.Read)
                {
                    Warning(item.name, "Local variable is never read.");
                }
            }
            EndScopeNoWarnings();
        }

        private void EndScopeNoWarnings()
        {
            _scopes.Pop();
        }

        private void Warning(Token name, string msg)
        {
            _resolverWarnings.Add(new ResolverWarning() { Token = name, Message = msg });
        }

        public void Visit(Stmt.Expression stmt)
        {
            Resolve(stmt.expression);
        }

        public object Visit(Expr.Function expr)
        {
            ResolveFunction(expr, FunctionType.Function);
            return null;
        }

        private void ResolveFunction(Expr.Function func, FunctionType functionType)
        {
            var enclosingFunctionType = _currentFunctionType;
            _currentFunctionType = functionType;
            var enclosingFunc = _currentExprFunc;
            _currentExprFunc = func;

            BeginScope();

            if (func.parameters != null)
            {
                for (int i = 0; i < func.parameters.Count; i++)
                {
                    var param = func.parameters[i];
                    Declare(param);
                    Define(param);
                }
            }

            Resolve(func.body);

            if (functionType == FunctionType.Init)
            {
                foreach (var initParam in _currentClass.init?.function?.parameters)
                {
                    ResolveLocal(initParam, true);
                }
            }

            EndScope();

            _currentFunctionType = enclosingFunctionType;
            _currentExprFunc = enclosingFunc;
        }

        public void Visit(Stmt.If stmt)
        {
            Resolve(stmt.condition);
            Resolve(stmt.thenBranch);
            if (stmt.elseBranch != null) Resolve(stmt.elseBranch);
        }

        public void Visit(Stmt.Return stmt)
        {
            if (_currentFunctionType == FunctionType.None)
            {
                throw new ResolverException(stmt.keyword, "Cannot return outside of a function.");
            }
            if (stmt.retVals != null)
            {
                Resolve(stmt.retVals);
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

        public void Visit(Stmt.MultiVar stmt)
        {
            if (stmt.initializer == null ||
                !(stmt.initializer is Expr.Call))
                throw new ResolverException(stmt.names[0], "MultiVar statement is being used but is not assigned to be initialised by a function.");

            if (stmt.initializer != null)
            {
                Resolve(stmt.initializer);
            }
            foreach (var item in stmt.names)
            {
                Declare(item);
                Define(item);
            }
        }

        public void Visit(Stmt.Function stmt)
        {
            Declare(stmt.name);
            Define(stmt.name);

            ResolveFunction(stmt.function, FunctionType.Function);
        }

        public void Visit(Stmt.While stmt)
        {
            Resolve(stmt.condition);
            Resolve(stmt.body);
            if (stmt.increment != null) Resolve(stmt.increment);
        }

        public void Visit(Stmt.Class stmt)
        {
            var enclosingClass = _currentClass;
            _currentClass = stmt;

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
                Resolve(stmt.superclass);
            }

            foreach (var item in stmt.metaFields)
            {
                Resolve(item);
            }

            foreach (Stmt.Function metaMeth in stmt.metaMethods)
            {
                ResolveFunction(metaMeth.function, FunctionType.Method);
            }

            BeginScope();
            foreach (var item in stmt.fields)
            {
                Resolve(item);
            }
            EndScopeNoWarnings();

            if (stmt.init != null)
                ResolveFunction(stmt.init.function, FunctionType.Init);

            _currentClass = enclosingClass;
        }

        public object Visit(Expr.Get expr)
        {
            Resolve(expr.targetObj);
            ResolveLocal(expr.name, true);

            return null;
        }

        public object Visit(Expr.Set expr)
        {
            Resolve(expr.val);
            Resolve(expr.targetObj);

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

        public object Visit(Expr.Throw expr)
        {
            if (expr.expr != null)
                Resolve(expr.expr);

            return null;
        }

        public void Visit(Stmt.Test stmt)
        {
            BeginScope();
            DeclareDefineRead("testName");
            Resolve(stmt.block);
            EndScopeNoWarnings();
        }

        public void Visit(Stmt.TestCase stmt)
        {
            if (stmt.valueGrouping != null)
                Resolve(stmt.valueGrouping);

            BeginScope();
            DeclareDefineRead("testCaseName");
            DeclareDefineRead("testValue");
            Resolve(stmt.block);
            EndScopeNoWarnings();
        }

        public object Visit(Expr.Variable expr)
        {
            if (_scopes.Count > 0 &&
                _scopes.Peek().localVariables.TryGetValue(expr.name.Lexeme, out var existingFlag) &&
                existingFlag.state == VariableUse.State.Declared)
            {
                throw new ResolverException(expr.name, "Can't read local variable in its own initializer.");
            }

            ResolveLocal(expr.name, true);
            return null;
        }

        public object Visit(Expr.Assign expr)
        {
            Resolve(expr.value);
            ResolveLocal(expr.name, false);
            return null;
        }
    }
}
