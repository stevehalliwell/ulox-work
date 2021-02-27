using System;
using System.Collections.Generic;

namespace ULox
{
    public class Class : Instance, ICallable
    {
        public const string InitalizerFunctionName = "init";

        private string _name;
        private List<Function> _methods;
        private Class _superclass;
        public Class Super => _superclass;
        private List<Stmt.Var> _vars;
        private List<short> _initVarIndexMatches;
        private Function _ourInitializer;
        private Function _initializer;

        public string Name => _name;

        public Class(
            Class metaClass,
            string name,
            Class superclass,
            List<Function> methods,
            List<Stmt.Var> fields,
            IEnvironment enclosing,
            List<short> initVarIndexMatches)
            : base(metaClass, enclosing)
        {
            _name = name;
            _methods = methods;
            _superclass = superclass;
            _vars = fields;
            _initVarIndexMatches = initVarIndexMatches;
            _ourInitializer = methods?.Find(x => x.Name == Class.InitalizerFunctionName);
            _initializer = _ourInitializer != null ? _ourInitializer : _superclass?._ourInitializer;
        }

        public virtual int Arity => (_initializer?.Arity - 1 ?? 0);

        public IReadOnlyList<Stmt.Var> Vars => _vars;

        public virtual object Call(Interpreter interpreter, FunctionArguments functionArgs)
        {
            var instance = new Instance(this, interpreter.CurrentEnvironment);

            //calling recursively pre-fix so most base class does its vars, then its child, then its child,
            //  and so on, will allow base class methods to use var index should they wish as ahead of time
            //  order is stable
            CreateFields(interpreter, instance, this);
            CreateMethods(interpreter, instance, this);

            if (_initializer != null)
            {
                var newArgs = new object[functionArgs.args.Length + 1];
                newArgs[0] = instance;
                functionArgs.args.CopyTo(newArgs, 1);
                functionArgs = FunctionArguments.New(newArgs);

                for (int i = 0; i < _initVarIndexMatches?.Count; i += 2)
                {
                    //we're using the indicies in pairs of out to in
                    instance.AssignSlot(_initVarIndexMatches[i + 1], functionArgs.At(_initVarIndexMatches[i]));
                }

                _initializer.Call(interpreter, functionArgs);
            }

            return instance;
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

        private void CreateMethods(Interpreter interpreter, Instance inst, Class fromClass)
        {
            if (fromClass == null)
                return;

            CreateMethods(interpreter, inst, fromClass._superclass);

            if (fromClass._methods != null)
            {
                foreach (var item in fromClass._methods)
                {
                    inst.Set(item.Name, item);
                }
            }
        }

        public override string ToString() => $"<class {_name}>";
    }
}
