using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public class Environment : IEnvironment
    {
        private IEnvironment _enclosing;
        protected Dictionary<string, short> valueIndicies = new Dictionary<string, short>();
        protected List<object> objectList = new List<object>();

        public IReadOnlyDictionary<string, short> ReadOnlyValueIndicies => valueIndicies;

        public Environment(IEnvironment enclosing)
        {
            _enclosing = enclosing;
        }

        public IEnvironment Enclosing
        {
            get => _enclosing;
            set => _enclosing = value;
        }

        public void AssignSlot(short slot, object val)
        {
            objectList[slot] = val;
        }

        public object FetchObject(short slot)
        {
            return objectList[slot];
        }

        public short DefineInAvailableSlot(string name, object value)
        {
            var ind = (short)(valueIndicies.Count() > 0 ? (valueIndicies.Values.Max() + 1) : 0);
            DefineSlot(name, ind, value);
            return ind;
        }

        public short FindSlot(string name)
        {
            if (valueIndicies.TryGetValue(name, out short ind))
                return ind;
            return -1;
        }

        public void DefineSlot(string name, short slot, object value)
        {
            if (valueIndicies.ContainsKey(name) ||
                valueIndicies.ContainsValue(slot))
            {
                throw new LoxException($"Environment value redefinition not allowed. Requested {name}:{slot} collided.");
            }

            while (objectList.Count < slot + 1) { objectList.Add(null); }

            valueIndicies[name] = slot;
            objectList[slot] = value;
        }

        public void ForEachValueName(System.Action<string> action)
        {
            foreach (var item in valueIndicies)
            {
                action(item.Key);
            }
        }
    }
}
