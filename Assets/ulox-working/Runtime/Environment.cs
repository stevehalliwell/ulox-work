using System;
using System.Collections.Generic;

namespace ULox
{
    //todo users then ask for depth and index if they have name, and store that for reuse
    public class Environment : IEnvironment
    {
        private IEnvironment _enclosing;
        protected Dictionary<string, short> valueIndicies = new Dictionary<string, short>();
        protected List<object> objectList = new List<object>();

        public Environment(IEnvironment enclosing)
        {
            _enclosing = enclosing;
        }

        public IEnvironment Enclosing => _enclosing;

        public void AssignSlot(short slot, object val)
        {
            objectList[slot] = val;
        }

        public object FetchObject(short slot)
        {
            return objectList[slot];
        }

        public short Define(string name, object value)
        {
            //todo add error
            short ind = (short)objectList.Count;
            valueIndicies.Add(name, ind);
            objectList.Add(value);
            return ind;
        }

        public short FindSlot(string name)
        {
            if (valueIndicies.TryGetValue(name, out short ind))
                return ind;
            return -1;
        }
    }
}
