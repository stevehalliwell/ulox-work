﻿using System;
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
                values[name.Lexeme] = val;

            if (enclosing != null)
                enclosing.Assign(name, val);

            throw new EnvironmentException(name, $"Undefined variable {name.Lexeme}");
        }
    }
}