using System;
using System.Collections.Generic;

namespace ULox
{
    public class Environment
    {
        public class EnvironmentException : TokenException
        {
            public EnvironmentException(Token token, string msg)
                 : base(token, msg)
            { }
        }

        private Dictionary<string, Object> values = new Dictionary<string, Object>();

        public void Define(String name, Object value)
        {
            values.Add(name, value);
        }

        public Object Get(Token name)
        {
            if (values.TryGetValue(name.Lexeme, out Object retval))
            {
                return retval;
            }

            throw new EnvironmentException(name,$"Undefined variable {name.Lexeme}");
        }

        public void Assign(Token name, object val)
        {
            if (values.ContainsKey(name.Lexeme))
                values[name.Lexeme] = val;

            throw new EnvironmentException(name, $"Undefined variable {name.Lexeme}");
        }
    }
}