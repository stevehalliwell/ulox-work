﻿using System;
using System.Collections.Generic;

namespace ULox
{
    public class Class : Instance, ICallable
    {
        public const string InitalizerFunctionName = "init";
        public const string ThisIdentifier = "this";
        public const string SuperIdentifier = "super";
        public static Token ThisToken = new Token(TokenType.THIS, "this", null, -1, -1);

        //presently closures go super->this->members
        //  These offsets exist so that if/when this changes, it's less tiresome to do so
        public const short ThisSlot = 0;
        public const short SuperSlot = 0;

        private string _name;
        private Dictionary<string, Function> _methods;
        public IReadOnlyDictionary<string, Function> ReadOnlyMethods => _methods;
        private Class _superclass;
        public Class Super => _superclass;
        private List<Stmt.Var> _vars;
        private Function _initializer;

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
            GenerateInitializerData();
        }

        private void GenerateInitializerData()
        {
            _initializer = FindMethod(InitalizerFunctionName);
        }

        public virtual int Arity => (_initializer?.Arity ?? Function.StartingParamSlot);

        public virtual object Call(Interpreter interpreter, FunctionArguments functionArgs)
        {
            var instance = new Instance(this, interpreter.CurrentEnvironment);

            //calling recursively pre-fix so most base class does its vars, then its child, then its child,
            //  and so on, will allow base class methods to use var index should they wish as ahead of time
            //  order is stable
            CreateFields(interpreter, instance, this);

            if (_initializer != null)
            {
                functionArgs.@this = instance;
                //todo shouldn't need the bind since it's being called
                _initializer.Bind(instance).Call(interpreter, functionArgs);
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
