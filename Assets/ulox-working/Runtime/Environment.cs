using System;
using System.Collections.Generic;

namespace ULox
{
    //todo store list of objects and dict of name to index
    //  users then ask for depth and index if they have name, and store that for reuse
    public class Environment : IEnvironment
    {
        private IEnvironment _enclosing;
        protected Dictionary<string, object> values = new Dictionary<string, object>();

        public Environment(IEnvironment enclosing)
        {
            _enclosing = enclosing;
        }

        public IEnvironment Enclosing => _enclosing;

        public void Assign(Token name, object val, bool checkEnclosing)
        {
            if (values.ContainsKey(name.Lexeme))
            {
                values[name.Lexeme] = val;
                return;
            }

            if (checkEnclosing && _enclosing != null)
            {
                _enclosing.Assign(name, val, true);
                return;
            }

            throw new EnvironmentException(name, $"Undefined variable {name.Lexeme}");
        }

        public void Assign(string tokenLexeme, object val, bool checkEnclosing)
        {
            if (values.ContainsKey(tokenLexeme))
            {
                values[tokenLexeme] = val;
                return;
            }

            if (checkEnclosing && _enclosing != null)
            {
                _enclosing.Assign(tokenLexeme, val, true);
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

        public object Fetch(Token name, bool checkEnclosing)
        {
            if (values.TryGetValue(name.Lexeme, out object retval))
            {
                return retval;
            }

            if (checkEnclosing && _enclosing != null)
                return _enclosing.Fetch(name, true);

            throw new EnvironmentException(name, $"Undefined variable {name.Lexeme}");
        }

        public object Fetch(string tokenLexeme, bool checkEnclosing)
        {
            if (values.TryGetValue(tokenLexeme, out object retval))
            {
                return retval;
            }

            if (checkEnclosing && _enclosing != null)
                return _enclosing.Fetch(tokenLexeme, true);

            throw new LoxException($"Undefined variable {tokenLexeme}");
        }
    }
}
