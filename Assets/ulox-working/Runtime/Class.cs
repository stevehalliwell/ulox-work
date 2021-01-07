using System.Collections.Generic;

namespace ULox
{
    //todo document that pure vars get merged down into child in same order, methods do not
    //  do super methods need to be aware of this?
    //todo is there a desire to get access to your class from your instance?
    public class Class : Instance, ICallable
    {
        //presently closures go super->this->members 
        //  These offsets exist so that if/when this changes, it's less tiresome to do so
        public const short ThisSlot = 0;
        public const short SuperSlot = 0;
        public const int StartingMemberSlot = 0;

        private string _name;
        private Dictionary<string, Function> _methods;
        private Class _superclass;
        public Class Super => _superclass;
        private List<Stmt.Var> _vars;

        public string Name => _name;

        public Class(
            Class metaClass,
            string name,
            Class superclass,
            Dictionary<string, Function> methods,
            List<Stmt.Var> fields,
            IEnvironment enclosing)
            : base(metaClass, enclosing)
        {
            _name = name;
            _methods = methods;
            _superclass = superclass;
            _vars = fields;
        }

        public virtual int Arity => FindMethod("init")?.Arity ?? 0;

        public virtual object Call(Interpreter interpreter, object[] args)
        {
            var instance = new Instance(this, interpreter.CurrentEnvironment);

            CreateFields(interpreter, instance, this);

            var initializer = FindMethod("init");
            if (initializer != null)
            {
                initializer.Bind(instance).Call(interpreter, args);
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
                }
            }
        }

        public virtual Function FindMethod(string lexeme)
        {
            if (_methods == null) return null;

            if (_methods.TryGetValue(lexeme, out Function func))
                return func;

            if (_superclass != null)
                return _superclass.FindMethod(lexeme);

            return null;
        }

        public override string ToString() => $"<class {_name}>";
    }
}
