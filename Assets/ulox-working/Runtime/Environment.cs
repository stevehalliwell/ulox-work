using System;
using System.Collections;
using System.Collections.Generic;

namespace ULox
{
    public class Environment : IEnvironment
    {
        private IEnvironment _enclosing;
        protected Dictionary<string, object> values = new Dictionary<string, object>();

        public Environment(IEnvironment enclosing)
        {
            _enclosing = enclosing;
        }

        public IEnvironment Enclosing { get => _enclosing; set => _enclosing = value; }

        public void Assign(string tokenLexeme, object val, bool canDefine, bool checkEnclosing)
        {
            if (values.ContainsKey(tokenLexeme))
            {
                values[tokenLexeme] = val;
                return;
            }

            if (canDefine)
            {
                Define(tokenLexeme, val);
                return;
            }

            if (checkEnclosing && _enclosing != null)
            {
                _enclosing.Assign(tokenLexeme, val, false, true);
                return;
            }

            throw new LoxException($"Undefined variable {tokenLexeme}");
        }

        public void Define(String name, object value)
        {
            values.Add(name, value);
        }

        public bool Exists(string address)
        {
            return values.ContainsKey(address);
        }

        private readonly object FAILURE_OBJECT = new object();

        public object Fetch(string tokenLexeme, bool checkEnclosing)
        {
            var res = FetchNoThrow(tokenLexeme, checkEnclosing, FAILURE_OBJECT);

            return res != FAILURE_OBJECT ? 
                res :
                throw new LoxException($"Undefined variable {tokenLexeme}");
        }

        public void VisitValues(Action<string, object> action)
        {
            foreach (var item in values)
            {
                action.Invoke(item.Key, item.Value);
            }
        }

        public object FetchNoThrow(string tokenLexeme, bool checkEnclosing, object defaultIfNotFound)
        {
            if (values.TryGetValue(tokenLexeme, out object retval))
            {
                return retval;
            }

            if (checkEnclosing && _enclosing != null)
                return _enclosing.Fetch(tokenLexeme, true);

            return defaultIfNotFound;
        }
    }
}
