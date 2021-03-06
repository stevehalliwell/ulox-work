﻿using System.Linq;

namespace ULox
{
    public abstract class TestLoxEngine
    {
        public string InterpreterResult { get; private set; } = string.Empty;
        public Engine _engine;

        protected void AppendResult(string str) => InterpreterResult += str;

        protected TestLoxEngine(params ILoxEngineLibraryBinder[] loxEngineLibraryBinders)
        {
            var binders = new ILoxEngineLibraryBinder[] { new LoxCoreLibrary(AppendResult) };
            _engine = new Engine(
                binders.Concat(loxEngineLibraryBinders).ToArray());
        }

        public virtual void Run(string testString, bool catchAndLogExceptions, bool logWarnings = true, System.Action<string> REPLPrint = null)
        {
            try
            {
                if (REPLPrint != null)
                    _engine.RunREPL(testString, REPLPrint);
                else
                    _engine.Run(testString);

                if (logWarnings)
                {
                    AppendResult(
                        string.Join(
                            "\n",
                            _engine.ResolverWarnings.Select(x => $"{x.Token} {x.Message}")
                                   ));
                }
            }
            catch (LoxException e)
            {
                if (catchAndLogExceptions)
                {
                    AppendResult(e.Message);
                }
                else
                {
                    throw;
                }
            }
            catch (System.ArgumentOutOfRangeException e)
            {
                if (catchAndLogExceptions)
                {
                    AppendResult(e.Message);
                }
                else
                {
                    throw;
                }
            }
            catch (System.IndexOutOfRangeException e)
            {
                if (catchAndLogExceptions)
                {
                    AppendResult(e.Message);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
