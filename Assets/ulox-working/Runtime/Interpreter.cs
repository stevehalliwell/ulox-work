using System;
using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public class Interpreter : Expr.Visitor<Object>,
                               Stmt.Visitor
    {
        public const string GlobalsIdentifier = "Globals";
        public const string NulIdentifier = "null";

        private Environment _globals = new Instance(null, null);
        public IEnvironment CurrentEnvironment => _environmentStack.Peek();
        private Stack<IEnvironment> _environmentStack = new Stack<IEnvironment>();
        private TestSuiteManager _testSuiteManager = new TestSuiteManager();

        public TestSuiteManager TestSuiteManager => _testSuiteManager;

        public Environment Globals => _globals;

        public Interpreter()
        {
            _environmentStack.Push(Globals);
            Globals.DefineInAvailableSlot(GlobalsIdentifier, Globals);
        }

        public IEnvironment PushNewEnvironemnt()
        {
            var ret = new Environment(CurrentEnvironment);
            _environmentStack.Push(ret);
            return ret;
        }

        public void PushEnvironemnt(IEnvironment env)
        {
            _environmentStack.Push(env);
        }

        public bool PopSpecificEnvironemnt(IEnvironment env)
        {
            if (_environmentStack.Peek() == env)
            {
                _environmentStack.Pop();
                return true;
            }
            return false;
        }

        public IEnvironment PopEnvironemnt()
        {
            return _environmentStack.Pop();
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
                    output?.Invoke(res?.ToString() ?? NulIdentifier);
                }
                else
                {
                    Execute(item);
                }
            }
        }

        public void ExecuteBlock(List<Stmt> statements, Environment environment)
        {
            try
            {
                _environmentStack.Push(environment);
                foreach (var stmt in statements)
                {
                    Execute(stmt);
                }
            }
            finally
            {
                _environmentStack.Pop();
            }
        }

        private void Execute(Stmt stmt) => stmt.Accept(this);

        public object Visit(Expr.Binary expr)
        {
            object left = Evaluate(expr.left);
            object right = Evaluate(expr.right);

            if(left is Instance leftInst)
            {
                var classOperator = leftInst.GetOperator(expr.op.TokenType);

                if(classOperator != null)
                {
                    return classOperator.Call(this, FunctionArguments.New(left, right));
                }
                else
                {
                    throw new ClassException(expr.op, "Did not find operator on left instance.");
                }
            }

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
                    
                    if(right is string rightS2)
                        return left.ToString() + rightS2;

                    throw new RuntimeTypeException(expr.op, "Operands must be numbers or strings.");
                }
                break;
            }

            return null;
        }

        public object Visit(Expr.Grouping expr)
        {
            //a number of things expect a grouping to resolve to a single number so we support that
            if (expr.expressions.Count == 1)
                return Evaluate(expr.expressions[0]);
            else
                return GroupingMultiEval(expr);
        }

        private object[] GroupingMultiEval(Expr.Grouping expr)
        {
            //todo would be nice not to need to alloc and array conver all of these
            var argList = new List<object>();   //todo cache alloc?
            foreach (var item in expr.expressions)
            {
                var res = Evaluate(item);
                if (res is object[] resArray)
                    argList.AddRange(resArray);
                else
                    argList.Add(res);
            }

            return argList.ToArray();
        }

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

        public static bool IsEqual(object left, object right)
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

        public void Visit(Stmt.Block stmt) => ExecuteBlock(stmt.statements, new Environment(CurrentEnvironment));

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

            var args = GroupingMultiEval(expr.arguments);

            if (callee is ICallable calleeCallable)
            {
                if (args.Length + Function.StartingParamSlot != calleeCallable.Arity)
                    throw new RuntimeCallException(expr.paren,
                        $"Expected { calleeCallable.Arity} args but got { args.Length }");

                //the callee will be a Method if it has a this
                return calleeCallable.Call(this, FunctionArguments.New(args));
            }

            throw new RuntimeTypeException(expr.paren, "Can only call function types");
        }

        public object Visit(Expr.Function expr)
        {
            //todo if it doesn't use closure does it need current env?
            return new Function(null, expr, CurrentEnvironment, false);
        }

        public void Visit(Stmt.Return stmt)
        {
            throw new Return(stmt.keyword, Visit(stmt.retVals));
        }

        public void Visit(Stmt.Var stmt)
        {
            object value = null;
            if (stmt.initializer != null)
            {
                value = Evaluate(stmt.initializer);
                if (value is object[] objArray)
                {
                    value = objArray[0];
                }
            }

            DefineVarInEnv(stmt, value);
        }

        public void Visit(Stmt.MultiVar stmt)
        {
            object[] initialiserResults = null;
            if (stmt.initializer != null)
            {
                var rawInitialiserResults = Evaluate(stmt.initializer);
                if (rawInitialiserResults is object[] objArray)
                {
                    initialiserResults = objArray;
                }
            }

            if (initialiserResults == null)
                throw new RuntimeTypeException(stmt.names[0], "MultiVar being used but was not given valid initialiser results from function.");

            for (int i = 0; i < stmt.names.Count; i++)
            {
                CurrentEnvironment.DefineInAvailableSlot(stmt.names[i].Lexeme, 
                    (i < initialiserResults.Length ? initialiserResults[i] : null));
            }

        }

        public void Visit(Stmt.Function stmt)
        {
            var func = new Function(stmt.name.Lexeme, stmt.function, CurrentEnvironment, false);
            DefineFunctionInEnv(stmt, func);
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
                CurrentEnvironment.DefineSlot(stmt.name.Lexeme, stmt.knownSlot, null);
            else
                stmt.knownSlot = CurrentEnvironment.DefineInAvailableSlot(stmt.name.Lexeme, null);
            
            if (stmt.superclass != null)
            {
                _environmentStack.Push(new Environment(CurrentEnvironment));
                CurrentEnvironment.DefineSlot(Class.SuperIdentifier, Class.SuperSlot, superclass);
            }

            var classMethods = new Dictionary<string, Function>();
            foreach (var method in stmt.metaMethods)
            {
                var func = new Function(method.name.Lexeme, method.function, CurrentEnvironment, false);
                classMethods[method.name.Lexeme] = func;
            }

            classMethods.TryGetValue(Class.InitalizerFunctionName, out var metaInit);

            var metaClass = new Class(null, stmt.name.Lexeme + "_meta", null, classMethods, null, null,null, metaInit);

            var methods = new Dictionary<string, Function>();
            foreach (Stmt.Function method in stmt.methods)
            {
                var function = new Function(
                    method.name.Lexeme,
                    method.function,
                    CurrentEnvironment,
                    method.name.Lexeme == Class.InitalizerFunctionName);

                methods[method.name.Lexeme] = function;
            }


            methods.TryGetValue(Class.InitalizerFunctionName, out var initFunc);

            var @class = new Class(
                metaClass,
                stmt.name.Lexeme,
                superclass,
                methods,
                stmt.fields,
                CurrentEnvironment,
                stmt.indexFieldMatches,
                initFunc);

            if (stmt.metaFields != null)
            {
                foreach (var item in stmt.metaFields)
                {
                    @class.Set(item.name.Lexeme, Evaluate(item.initializer));
                }
            }

            if (superclass != null)
            {
                _environmentStack.Pop();
            }

            CurrentEnvironment.AssignSlot(stmt.knownSlot, @class);
        }

        public object Visit(Expr.Get expr)
        {
            if (expr.targetObj == null)
            {
                // it as a variable then
                if (expr.varLoc != EnvironmentVariableLocation.Invalid)
                {
                    return CurrentEnvironment.Ancestor(expr.varLoc.depth).FetchObject(expr.varLoc.slot);
                }
                else
                {
                    try
                    {
                        expr.varLoc = CurrentEnvironment.FindLocation(expr.name.Lexeme);
                        return CurrentEnvironment.Ancestor(expr.varLoc.depth).FetchObject(expr.varLoc.slot);
                    }
                    catch (LoxException)
                    {
                        if (!AttemptToResolveToMember(expr))
                            throw new EnvironmentException(expr.name, $"Undefined variable {expr.name.Lexeme}");
                    }
                }
            }

            var obj = Evaluate(expr.targetObj);

            if(obj == null)
            {
                throw new RuntimeAccessException(expr.name, "Evaluation resulted in null.");
            }

            if (obj is Instance objInst)
            {
                object result = null;

                if(expr.varLoc.slot == EnvironmentVariableLocation.InvalidSlot)
                {
                    expr.varLoc.slot = objInst.FindSlot(expr.name.Lexeme);
                }
                if(expr.varLoc.slot != EnvironmentVariableLocation.InvalidSlot)
                {
                    result = objInst.FetchObject(expr.varLoc.slot);
                }
                else
                {
                    //todo this dict lookup in here is still a cause of much perf issues
                    result = objInst.GetMethod(expr.name);
                }

                if (result is IFunction resultFunc)
                {
                    if (resultFunc.IsGetter)
                    {
                        result = resultFunc.Call(this, FunctionArguments.New(objInst));
                    }
                }
                return result;
            }

            throw new RuntimeTypeException(expr.name, "Only instances have properties.");
        }

        public object Visit(Expr.Set expr)
        {
            if (expr.targetObj == null)
            {
                //attempt to use as an assign
                if (expr.varLoc != EnvironmentVariableLocation.Invalid)
                {
                    var assignVal = Evaluate(expr.val);
                    CurrentEnvironment.Ancestor(expr.varLoc.depth).AssignSlot(expr.varLoc.slot, assignVal);
                    return assignVal;
                }
                else
                {
                    try
                    {
                        expr.varLoc = CurrentEnvironment.FindLocation(expr.name.Lexeme);
                        var assignVal = Evaluate(expr.val);
                        CurrentEnvironment.Ancestor(expr.varLoc.depth).AssignSlot(expr.varLoc.slot, assignVal);
                        return assignVal;
                    }
                    catch (LoxException)
                    {
                        if (!AttemptToResolveToMember(expr))
                            throw new EnvironmentException(expr.name, $"Undefined variable {expr.name.Lexeme}");
                    }
                }
            }

            if(expr.targetObj is Expr.Grouping grouping)
            {
                return HandleMultiSet(expr, grouping);
            }

            
            var obj = Evaluate(expr.targetObj) as Instance;
            var val = Evaluate(expr.val);

            if (obj == null)
            {
                throw new RuntimeTypeException(expr.name, "Only instances have fields.");
            }

            //todo this dict lookup in here is still a cause of much perf issues
            obj.Set(expr.name.Lexeme, val);
            return val;
        }

        private object HandleMultiSet(Expr.Set setExpr, Expr.Grouping grouping)
        {
            object[] resultsFromFunc = System.Array.Empty<object>();
            //we need the results first.
            var rawFunctionReturn = Evaluate(setExpr.val);
            if(rawFunctionReturn is object[] retVals)
            {
                resultsFromFunc = retVals;
            }
            else if (rawFunctionReturn is object retVal)
            {
                resultsFromFunc = new object[] { retVal }; //todo cache alloc?
            }

            for (int i = 0; i < grouping.expressions.Count; i++)
            {
                var curExpr = grouping.expressions[i] as Expr.Get;

                if (i < resultsFromFunc.Length)
                {
                    var val = resultsFromFunc[i];
                    Visit(curExpr);  //that'll locate
                    if(curExpr.targetObj == null)
                    {
                        CurrentEnvironment.Ancestor(curExpr.varLoc.depth).AssignSlot(curExpr.varLoc.slot, val);
                    }
                    else
                    {
                        var obj = Evaluate(curExpr.targetObj) as Instance;
                        obj.Set(curExpr.name.Lexeme, val);
                    }
                }
            }

            return resultsFromFunc;
        }

        public object Visit(Expr.This expr)
        {
            if (expr.varLoc == EnvironmentVariableLocation.Invalid)
            {
                try
                {
                    expr.varLoc = CurrentEnvironment.FindLocation(expr.keyword.Lexeme);
                }
                catch (LoxException)
                {
                    throw new EnvironmentException(expr.keyword, $"Undefined variable {expr.keyword.Lexeme}");
                }
            }
            return CurrentEnvironment.Ancestor(expr.varLoc.depth).FetchObject(expr.varLoc.slot);
        }

        public object Visit(Expr.Super expr)
        {
            if (expr.thisVarLoc == EnvironmentVariableLocation.Invalid)
                expr.thisVarLoc = CurrentEnvironment.FindLocation(Class.ThisIdentifier);

            var inst = (Instance)CurrentEnvironment.Ancestor(expr.thisVarLoc.depth).FetchObject(expr.thisVarLoc.slot);

            if (expr.superVarLoc == EnvironmentVariableLocation.Invalid)
                expr.superVarLoc = CurrentEnvironment.FindLocation(Class.SuperIdentifier);

            var superclass = (Class)CurrentEnvironment.Ancestor(expr.superVarLoc.depth).FetchObject(expr.superVarLoc.slot);

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



        private bool AttemptToResolveToMember(Expr.Get expr)
        {
            var localThisVar = CurrentEnvironment.FetchObject(Class.ThisSlot);
            if (expr.varLoc == EnvironmentVariableLocation.Invalid && localThisVar is Instance localThis)
            {
                var matchingLoc = localThis.FindSlot(expr.name.Lexeme);
                if (matchingLoc != EnvironmentVariableLocation.InvalidSlot)
                {
                    expr.targetObj = new Expr.This(Class.MakeThisToken(expr.name),
                        EnvironmentVariableLocation.Invalid); ;
                    expr.varLoc.slot = matchingLoc;
                    return true;
                }
            }
            return false;
        }

        private bool AttemptToResolveToMember(Expr.Set expr)
        {
            var localThisVar = CurrentEnvironment.FetchObject(Class.ThisSlot);
            if (expr.varLoc == EnvironmentVariableLocation.Invalid && localThisVar is Instance localThis)
            {
                var matchingLoc = localThis.FindSlot(expr.name.Lexeme);
                if (matchingLoc != EnvironmentVariableLocation.InvalidSlot)
                {
                    expr.targetObj = new Expr.This(Class.MakeThisToken(expr.name),
                        EnvironmentVariableLocation.Invalid);
                    expr.varLoc.slot = matchingLoc;
                    return true;
                }
            }
            return false;
        }

        private void DefineFunctionInEnv(Stmt.Function stmt, Function func)
        {
            if (stmt.knownSlot != EnvironmentVariableLocation.InvalidSlot)
                CurrentEnvironment.DefineSlot(stmt.name.Lexeme, stmt.knownSlot, func);
            else
                CurrentEnvironment.DefineInAvailableSlot(stmt.name.Lexeme, func);
        }

        private void DefineVarInEnv(Stmt.Var stmt, object value)
        {
            if (stmt.knownSlot != EnvironmentVariableLocation.InvalidSlot)
                CurrentEnvironment.DefineSlot(stmt.name.Lexeme, stmt.knownSlot, value);
            else
                CurrentEnvironment.DefineInAvailableSlot(stmt.name.Lexeme, value);
        }

        public object Visit(Expr.Throw expr)
        {
            throw new RuntimeException(expr.keyword, Evaluate(expr.expr)?.ToString());
        }

        public void Visit(Stmt.Test stmt)
        {
            PushNewEnvironemnt().DefineInAvailableSlot("testName", stmt.name.Lexeme);
            _testSuiteManager.SetSuite(stmt.name.Lexeme);
            Visit(stmt.block);
            _testSuiteManager.EndCurrentSuite();
            PopEnvironemnt();
        }

        public void Visit(Stmt.TestCase stmt)
        {
            if (stmt.valueGrouping == null)
            {
                RunTestCaseInternal(stmt, null);
            }
            else
            {
                var exprResArr = GroupingMultiEval(stmt.valueGrouping);
                foreach (var valueExpr in exprResArr)
                {
                    RunTestCaseInternal(stmt, valueExpr);
                }
            }
        }

        private void RunTestCaseInternal(Stmt.TestCase stmt, object valueExpr)
        {
            try
            {
                _testSuiteManager.StartCase(stmt, valueExpr);

                var env = PushNewEnvironemnt();
                env.DefineInAvailableSlot("testCaseName", stmt.name.Lexeme);
                env.DefineInAvailableSlot("testValue", valueExpr);
                Visit(stmt.block);
            }
            catch (LoxException e)
            {
                _testSuiteManager.LoxExceptionWasThrownByCase(e);
                if (!_testSuiteManager.IsInUserSuite)
                    throw;
            }
            finally
            {
                PopEnvironemnt();
                _testSuiteManager.EndingCase();
            }
        }
    }
}
