using System.Runtime.InteropServices;

namespace ULox
{
    public struct Value
    {
        public enum Type
        {
            Null,
            Double,
            Bool,
            String,
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DataUnion
        {
            [FieldOffset(0)]
            public double asDouble;
            [FieldOffset(0)]
            public bool asBool;
            [FieldOffset(0)]
            public string asString;
        }

        public Type type;
        public DataUnion val;

        public bool IsFalsey 
        {
            get 
            {
                return type == Type.Null || 
                    (type == Type.Bool && val.asBool == false);
            }
        }

        public override string ToString() 
        {
            switch(type)
            {
            case Type.Double:
                return val.asDouble.ToString();
            case Type.Bool:
                return val.asBool.ToString();
            case Type.String:
                return val.asString?.ToString() ?? "null";
            default:
                return "null";
            }
        }

        public static Value New(double val) 
            => new Value() { type = Type.Double, val = new DataUnion() { asDouble = val} };

        public static Value New(bool val) 
            => new Value() { type = Type.Bool, val = new DataUnion() { asBool = val } };

        public static Value New(string val)
            => new Value() { type = Type.String, val = new DataUnion() { asString = val } };

        public static Value Null()
            => new Value() { type = Type.Null };
    }
}
