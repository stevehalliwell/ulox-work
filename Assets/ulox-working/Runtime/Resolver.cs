using System;
using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public class Resolver : Expr.Visitor<Object>,
                                Stmt.Visitor
    {
        private class VariableUse
        {
            public Token name;

            public enum State { Declared, Defined, Read };

            public State state;
            public short slot;

            public VariableUse(Token Name, State _state, short _slot)
            {
                name = Name;
                state = _state;
                slot = _slot;
            }
        }

        private class ScopeInfo
        {
            public Dictionary<string, VariableUse> localVariables = new Dictionary<string, VariableUse>();
            public List<int> fetchedVariableDistances = new List<int>();
            public bool HasLocals => localVariables.Count > 0;
            public bool HasClosedOverVars => fetchedVariableDistances.Count(x => x > 0) > 0;
        }

        private List<ScopeInfo> _scopes = new List<ScopeInfo>();
        private FunctionType _currentFunctionType = FunctionType.NONE;
        private Expr.Function _currentExprFunc;
        private Stmt.Class _currentClass;
        private List<ResolverWarning> _resolverWarnings = new List<ResolverWarning>();

        public List<ResolverWarning> ResolverWarnings => _resolverWarnings;

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

        public Resolver()
        {
            Reset();
        }

        public void Reset()
        {
            _scopes.Clear();

            _currentFunctionType = FunctionType.NONE;
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

        public object Visit(Expr.Assign expr)
        {
            Resolve(expr.value);
            expr.varLoc = ResolveLocal(expr, expr.name, false);
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
            if (_scopes.Count > 0 &&
                _scopes.Last().localVariables.TryGetValue(expr.name.Lexeme, out var existingFlag) &&
                existingFlag.state == VariableUse.State.Declared)
            {
                throw new ResolverException(expr.name, "Can't read local variable in its own initializer.");
            }

            expr.varLoc = ResolveLocal(expr, expr.name, true);
            return null;
        }

        public void Visit(Stmt.Block stmt)
        {
            BeginScope();
            Resolve(stmt.statements);
            EndScope();
        }

        private EnvironmentVariableLocation ResolveLocal(Expr expr, Token name, bool isRead)
        {
            EnvironmentVariableLocation retval = EnvironmentVariableLocation.Invalid;
            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                if (_scopes[i].localVariables.TryGetValue(name.Lexeme, out var varUse))
                {
                    if (isRead)
                    {
                        varUse.state = VariableUse.State.Read;
                    }

                    retval = new EnvironmentVariableLocation()
                    {
                        depth = (ushort)(_scopes.Count - i - 1),
                        slot = varUse.slot
                    };
                    break;
                }
            }

            if (_scopes.Count > 0)
                _scopes.Last().fetchedVariableDistances.Add(retval.depth);

            return retval;
        }

        private void BeginScope()
        {
            _scopes.Add(new ScopeInfo());
        }

        private short Declare(Token name)
        {
            if (_scopes.Count == 0) return EnvironmentVariableLocation.InvalidSlot;

            var scope = _scopes.Last();
            if (scope.localVariables.ContainsKey(name.Lexeme))
            {
                throw new ResolverException(name, "Already a variable with this name in this scope.");
            }

            var slot = (short)scope.localVariables.Count;
            scope.localVariables.Add(name.Lexeme, new VariableUse(name, VariableUse.State.Declared, slot));
            return slot;
        }

        private void DeclareAt(Token name, short slot)
        {
            if (_scopes.Count == 0) return;

            var scope = _scopes.Last();
            if (scope.localVariables.ContainsKey(name.Lexeme))
            {
                throw new ResolverException(name, "Already a variable with this name in this scope.");
            }
            if (scope.localVariables.Values.FirstOrDefault(x => x.slot == slot) != default)
            {
                throw new ResolverException(name, $"Already a variable at slot {slot} in this scope.");
            }

            scope.localVariables.Add(name.Lexeme, new VariableUse(name, VariableUse.State.Declared, slot));
        }

        private void DefineManually(string name, short slot)
        {
            if (_scopes.Count == 0) return;
            var scope = _scopes.Last();

            if (scope.localVariables.ContainsKey(name) ||
                scope.localVariables.Values.FirstOrDefault(x => x.slot == slot) != default)
            {
                throw new LoxException($"Already a variable of name {name} or at slot {slot} in this scope.");
            }

            scope.localVariables[name] = new VariableUse(new Token(TokenType.IDENTIFIER, name, name, -1, -1), VariableUse.State.Read, slot);
        }

        private void Define(Token name)
        {
            if (_scopes.Count == 0) return;
            var count = _scopes.Last().localVariables.Count;
            _scopes.Last().localVariables[name.Lexeme].state = VariableUse.State.Defined;
        }

        private void EndScope()
        {
            foreach (var item in _scopes.Last().localVariables.Values)
            {
                if (item.state != VariableUse.State.Read)
                {
                    if (item.name.TokenType == TokenType.THIS ||
                        item.name.TokenType == TokenType.SUPER)
                        continue;

                    Warning(item.name, "Local variable is never read.");
                }
            }
            EndScopeNoWarnings();
        }

        private void EndScopeNoWarnings()
        {
            _scopes.RemoveAt(_scopes.Count - 1);
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
            ResolveFunction(expr, FunctionType.FUNCTION);
            return null;
        }

        //todo warn when a function isn't a meta but doesn't use anything from the closure
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
                    DeclareAt(param, (short)i);
                    Define(param);
                }
            }
            else if(functionType == FunctionType.METHOD)
            {
                //we are in a get
                DeclareAt(Class.ThisToken, Class.ThisSlot);
            }

            Resolve(func.body);

            func.NeedsClosure = _scopes.Count > 0 ? _scopes.Last().HasClosedOverVars : true;
            func.HasLocals = _scopes.Count > 0 ? _scopes.Last().HasLocals : true;
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
            if (_currentFunctionType == FunctionType.NONE)
            {
                throw new ResolverException(stmt.keyword, "Cannot return outside of a function.");
            }
            if (stmt.value != null)
            {
                if (_currentFunctionType == FunctionType.INITIALIZER)
                    throw new ResolverException(stmt.keyword, "Cannot return a value from an initializer");

                Resolve(stmt.value);
            }

            if (_currentExprFunc != null) _currentExprFunc.HasReturns = true;
        }

        public void Visit(Stmt.Var stmt)
        {
            stmt.knownSlot = Declare(stmt.name);
            if (stmt.initializer != null)
            {
                Resolve(stmt.initializer);
            }
            Define(stmt.name);
        }

        public void Visit(Stmt.Function stmt)
        {
            stmt.knownSlot = Declare(stmt.name);
            Define(stmt.name);

            ResolveFunction(stmt.function, FunctionType.FUNCTION);
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

            stmt.knownSlot = Declare(stmt.name);
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
                BeginScope();
                DefineManually(Class.ThisIdentifier, Class.ThisSlot);
                //Declare(metaMeth.name);
                //Define(metaMeth.name);
                ResolveFunction(metaMeth.function, FunctionType.METHOD);
                EndScope();
            }

            if (stmt.superclass != null)
            {
                BeginScope();
                DefineManually(Class.SuperIdentifier, Class.SuperSlot);
            }

            //BeginScope();
            //DefineManually(Class.ThisIdentifier, Class.ThisSlot);

            BeginScope();
            foreach (var item in stmt.fields)
            {
                Resolve(item);
            }
            EndScopeNoWarnings();

            foreach (Stmt.Function thisMeth in stmt.methods)
            {
                FunctionType declaration = FunctionType.METHOD;
                if (thisMeth.name.Lexeme == Class.InitalizerFunctionName)
                {
                    declaration = FunctionType.INITIALIZER;
                }
                //Declare(thisMeth.name);
                //Define(thisMeth.name);
                //todo can potentially locate and optimise this->get and this->set since we know some of the offsets
                ResolveFunction(thisMeth.function, declaration);
            }

            //EndScope();

            if (stmt.superclass != null) EndScope();

            //todo determine and save offsets of the init params to members by names, saving index
            //allowing this explicit
            // class Test{var a,b,c; init(a,b,c){this.a = a; this.b = b; this.c = c;}}
            // to be this implicitly
            //class Test{var a,b,c; init(a,b,c){}}

            _currentClass = enclosingClass;
        }

        public object Visit(Expr.Get expr)
        {
            //todo if in class and method and obj is this, we can attempt to cache the offset to this in the env and the offset from
            //  the instance to the variable
            Resolve(expr.obj);
            ResolveLocal(expr, expr.name, true);
            return null;
        }

        public object Visit(Expr.Set expr)
        {
            //todo if in class and method and obj is this, we can attempt to cache the offset to this in the env and the offset from
            //  the instance to the variable
            Resolve(expr.val);
            Resolve(expr.obj);
            return null;
        }

        public object Visit(Expr.This expr)
        {
            if (_currentClass == null)
                throw new ResolverException(expr.keyword, "Cannot use 'this' outside of a class.");

            expr.varLoc = ResolveLocal(expr, expr.keyword, false);
            return null;
        }

        public object Visit(Expr.Super expr)
        {
            if (_currentClass == null)
                throw new ResolverException(expr.keyword, "Cannot use 'super' outside of a class.");
            if (_currentClass.superclass == null)
                throw new ResolverException(expr.keyword, "Cannot use 'super' in a class with no superclass.");

            //todo it would be possible to keep a class tree and confirm if the method identifier exists on the super

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
