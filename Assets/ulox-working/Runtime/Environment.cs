using System;
using System.Collections.Generic;

namespace ULox
{
    //todo users then ask for depth and index if they have name, and store that for reuse
    public class Environment : IEnvironment
    {
        private IEnvironment _enclosing;
        protected Dictionary<string, int> valueIndicies = new Dictionary<string, int>();
        protected List<object> objectList = new List<object>();

        public Environment(IEnvironment enclosing)
        {
            _enclosing = enclosing;
        }

        public IEnvironment Enclosing => _enclosing;

        public void AssignIndex(int index, object val)
        {
            objectList[index] = val;
        }

        public object FetchIndex(int index)
        {
            return objectList[index];
        }

        public int AssignT(Token name, object val, bool checkEnclosing)
        {
            if (valueIndicies.TryGetValue(name.Lexeme, out var index))
            {
                objectList[index] = val;
                return index;
            }

            if (checkEnclosing && _enclosing != null)
            {
                return _enclosing.AssignT(name, val, true);
            }

            throw new EnvironmentException(name, $"Undefined variable {name.Lexeme}");
        }

        public int Assign(string tokenLexeme, object val, bool checkEnclosing)
        {
            if (valueIndicies.TryGetValue(tokenLexeme, out var index))
            {
                objectList[index] = val;
                return index;
            }

            if (checkEnclosing && _enclosing != null)
            {
                return _enclosing.Assign(tokenLexeme, val, true);
            }

            throw new LoxException($"Undefined variable {tokenLexeme}");
        }

        public int Define(String name, object value)
        {
            var ind = objectList.Count;
            valueIndicies.Add(name, ind);
            objectList.Add(value);
            return ind;
        }

        public int FetchIndex(string name)
        {
            if (valueIndicies.TryGetValue(name, out var ind))
                return ind;
            return -1;
        }

        public object FetchT(Token name, bool checkEnclosing)
        {
            if (valueIndicies.TryGetValue(name.Lexeme, out int index))
            {
                return objectList[index];
            }

            if (checkEnclosing && _enclosing != null)
                return _enclosing.FetchT(name, true);

            throw new EnvironmentException(name, $"Undefined variable {name.Lexeme}");
        }

        public object Fetch(string tokenLexeme, bool checkEnclosing)
        {
            if (valueIndicies.TryGetValue(tokenLexeme, out int index))
            {
                return objectList[index];
            }

            if (checkEnclosing && _enclosing != null)
                return _enclosing.Fetch(tokenLexeme, true);

            throw new LoxException($"Undefined variable {tokenLexeme}");
        }
    }
}
