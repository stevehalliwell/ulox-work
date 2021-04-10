using System.Collections.Generic;

namespace ULox
{
    public class Class : Instance, ICallable
    {
        public const string InitalizerFunctionName = "init";
        public const string InitalizerParamZeroName = "self";

        private string _name;
        private Class _superclass;
        public Class Super => _superclass;
        private List<Stmt.Var> _vars;
        private Function _initializer;

        public string Name => _name;

        public static Expr.Function EmptyInitFuncExpr()
        {
            return new Expr.Function(
                       new List<Token>() { new Token().Copy(TokenType.IDENTIFIER, Class.InitalizerParamZeroName) },
                       new List<Stmt>());
        }

        public Class(
            string name,
            Class superclass,
            Function init,
            List<Stmt.Var> fields,
            IEnvironment enclosing)
            : base(null)
        {
            _name = name;
            _superclass = superclass;
            _vars = fields;
            _initializer = init;

            if (_initializer == null)
            {
                _initializer = new Function(Class.InitalizerFunctionName, EmptyInitFuncExpr());
            }
        }

        public virtual int Arity => (_initializer?.Arity - 1 ?? 0);

        public IReadOnlyList<Stmt.Var> Vars => _vars;

        public virtual object Call(Interpreter interpreter, FunctionArguments functionArgs)
        {
            var instance = new Instance(this);

            //calling recursively pre-fix so most base class does its vars, then its child, then its child,
            //  and so on, will allow base class methods to use var index should they wish as ahead of time
            //  order is stable
            CreateFields(interpreter, instance, this);

            var newArgs = functionArgs.args;
            newArgs.Insert(0, instance);
            functionArgs = FunctionArguments.New(newArgs);

            var paramList = _initializer.Params;
            for (int i = 0; i < paramList.Count; i++)
            {
                instance.AssignIfExists(paramList[i].Lexeme, functionArgs.At(i));
            }

            CallInits(interpreter, instance, functionArgs, this);

            return instance;
        }

        private void CallInits(Interpreter interpreter, Instance instance, FunctionArguments functionArgs, Class fromClass)
        {
            if (fromClass == null)
                return;

            CallInits(interpreter, instance, functionArgs, fromClass._superclass);

            fromClass._initializer.Call(interpreter, functionArgs);
        }

        private void CreateFields(Interpreter interpreter, Instance inst, Class fromClass)
        {
            if (fromClass == null)
                return;

            CreateFields(interpreter, inst, fromClass._superclass);

            if (fromClass._vars != null)
            {
                foreach (var item in fromClass._vars)
                {
                    inst.Set(item.name.Lexeme, interpreter.Evaluate(item.initializer));
                    //this can be direct as there shouldn't be dups
                    //inst.DefineInAvailableSlot(item.name.Lexeme, interpreter.Evaluate(item.initializer));
                }
            }
        }

        public override string ToString() => $"<class {_name}>";
    }
}
