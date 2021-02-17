﻿using System;

namespace ULox
{
    public class LoxCoreLibrary : ILoxEngineLibraryBinder
    {
        private Action<string> _logger;

        private void PrintViaLogger(object o) => _logger?.Invoke(o?.ToString() ?? Interpreter.NulIdentifier);

        public LoxCoreLibrary(Action<string> logger)
        {
            _logger = logger;
        }

        public void BindToEngine(Engine engine)
        {
            engine.SetValue("print", 
                new Callable(1, (args) => PrintViaLogger(args.At(0))));
            engine.SetValue("printr", 
                new Callable(1, (args) =>
                {
                    const int PRINTR_DEPTH = 10;
                    var sb = new System.Text.StringBuilder();
                    PrintRecursive(args.At(0), sb, string.Empty, PRINTR_DEPTH);
                    PrintViaLogger(sb.ToString());
                }));
            engine.SetValue("clock", 
                new Callable(() => System.DateTime.Now.Ticks));
            engine.SetValue("sleep", 
                new Callable(1, (args) => System.Threading.Thread.Sleep((int)args.At<double>(0))));
            engine.SetValue("abort", 
                new Callable(() => throw new LoxException("abort")));
            engine.SetValue("panic",
                new Callable(() => throw new PanicException()));
            engine.SetValue("classof",
                new Callable(1, (objs) =>
                {
                    if (objs.At(0) is Instance inst)
                    {
                        return inst.Class;
                    }
                    throw new LoxException("'classof' can only be called on instances.");
                }));
        }

        private void PrintRecursive(object v, System.Text.StringBuilder sb, string prefix, int remainingDepth)
        {
            remainingDepth--;
            if (remainingDepth < 0)
            {
                sb.AppendLine("MAX_DEPTH_REACHED");
                return;
            }

            sb.Append(v?.ToString() ?? Interpreter.NulIdentifier);
            prefix += "  ";

            if (v is Class vClass)
            {
                //todo check for super/meta class
                if (vClass.Super != null)
                {
                    sb.AppendLine();
                    sb.Append(prefix);
                    sb.Append("meta : ");
                    PrintRecursive(vClass.Super, sb, prefix, remainingDepth);
                }

                foreach (var meth in vClass.ReadOnlyMethods)
                {
                    sb.AppendLine(); 
                    sb.Append(prefix);
                    PrintRecursive(meth.Value, sb, prefix, remainingDepth);
                }
            }
            else if (v is Environment vEnv)
            {
                foreach (var val in vEnv.ReadOnlyValueIndicies)
                {
                    sb.AppendLine();
                    sb.Append(prefix);
                    sb.Append(val.Key);
                    sb.Append(" : ");
                    PrintRecursive(vEnv.FetchObject(val.Value), sb, prefix, remainingDepth);
                }
            }
        }
    }
}
