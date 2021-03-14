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
            Function,
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
            [FieldOffset(0)]
            public Chunk asChunk;
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
            return type switch
            {
                Type.Double => val.asDouble.ToString(),
                Type.Bool => val.asBool.ToString(),
                Type.String => val.asString?.ToString() ?? "null",
                Type.Function => $"<fn {val.asChunk.Name}>",
                _ => "null",
            };
        }

        public static Value New(double val) 
            => new Value() { type = Type.Double, val = new DataUnion() { asDouble = val} };

        public static Value New(bool val) 
            => new Value() { type = Type.Bool, val = new DataUnion() { asBool = val } };

        public static Value New(string val)
            => new Value() { type = Type.String, val = new DataUnion() { asString = val } };

        public static Value New(Chunk val)
            => new Value() { type = Type.Function, val = new DataUnion() { asChunk = val } };

        public static Value Null()
            => new Value() { type = Type.Null };
    }
}
