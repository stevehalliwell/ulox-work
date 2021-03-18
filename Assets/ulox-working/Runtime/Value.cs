using System.Runtime.InteropServices;

namespace ULox
{
    public class ClosureInternal
    {
        public Chunk chunk;
    }

    public struct Value
    {
        public enum Type
        {
            Null,
            Double,
            Bool,
            String,
            Chunk,
            NativeFunction,
            Closure,
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
            [FieldOffset(0)]
            public System.Func<VM, int, Value> asNativeFunc;
            [FieldOffset(0)]
            public ClosureInternal asClosure;
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
                Type.Chunk => $"<fn {val.asChunk.Name}>",
                Type.NativeFunction => "<NativeFunc>",
                Type.Closure => $"<closure {val.asClosure.chunk.Name}>",
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
            => new Value() { type = Type.Chunk, val = new DataUnion() { asChunk = val } };

        public static Value New(System.Func<VM, int, Value> val)
            => new Value() { type = Type.NativeFunction, val = new DataUnion() { asNativeFunc = val } };

        public static Value New(ClosureInternal val)
            => new Value() { type = Type.Closure, val = new DataUnion() { asClosure = val } };

        public static Value Null()
            => new Value() { type = Type.Null };
    }
}
