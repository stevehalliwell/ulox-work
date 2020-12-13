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
        private Environment enclosing;

        public Environment() { }

        public Environment(Environment enclosing)
        {
            this.enclosing = enclosing;
        }

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

            if(enclosing != null) 
                return enclosing.Get(name);

            throw new EnvironmentException(name,$"Undefined variable {name.Lexeme}");
        }

        public void Assign(Token name, object val)
        {
            if (values.ContainsKey(name.Lexeme))
            {
                values[name.Lexeme] = val;
                return;
            }

            if (enclosing != null)
            {
                enclosing.Assign(name, val);
                return;
            }

            throw new EnvironmentException(name, $"Undefined variable {name.Lexeme}");
        }

        public object GetAt(int distance, Token name)
        {
            if (Ancestor(distance).values.TryGetValue(name.Lexeme, out object retval))
                return retval;

            throw new EnvironmentException(name, $"Undefined variable {name.Lexeme}");
        }

        public object GetAtDirect(int distance, string nameLexeme)
        {
            return Ancestor(distance).values[nameLexeme];
        }

        public Environment Ancestor(int distnace)
        {
            var ret = this;
            for (int i = 0; i < distnace; i++)
            {
                ret = ret.enclosing;
            }

            return ret;
        }

        public void AssignAt(int distance, Token name, object val)
        {
            Ancestor(distance).values[name.Lexeme] = val;
        }
    }
}