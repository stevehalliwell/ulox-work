using System.Runtime.InteropServices;

namespace ULox
{
    public class ClosureInternal
    {
        public Chunk chunk;
        public Value[] upvalues;
    }
    public class UpvalueInternal
    {
        public int index = -1;
        public bool isClosed = false;
        public Value value = Value.Null();
    }
    public class ClassInternal
    {
        public string name;
        public Table methods = new Table();
    }
    public class InstanceInternal
    {
        public ClassInternal fromClass;
        public Table fields = new Table();
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
            Upvalue,
            Class,
            Instance,
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
            [FieldOffset(0)]
            public UpvalueInternal asUpvalue;
            [FieldOffset(0)]
            public ClassInternal asClass;
            [FieldOffset(0)]
            public InstanceInternal asInstance;
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
            switch (type)
            {
            case Type.Null:
                return "null";
            case Type.Double:
                return val.asDouble.ToString();
            case Type.Bool:
                return val.asBool.ToString();
            case Type.String:
                return val.asString?.ToString() ?? "null";
            case Type.Chunk:
                var chunk = val.asChunk;
                if (chunk == null)
                    throw new System.Exception("Null Chunk in Value.ToString. Illegal.");
                var name = chunk.Name;
                return "<fn " + name + "> ";
            case Type.NativeFunction:
                return "<NativeFunc>";
            case Type.Closure:
                return $"<closure {val.asClosure.chunk.Name} upvals:{val.asClosure.upvalues.Length}>";
            case Type.Upvalue:
                return $"<upvalue {val.asUpvalue.index}>";
            case Type.Class:
                return $"<class {val.asClass.name}>";
            case Type.Instance:
                return $"<inst {val.asInstance.fromClass.name}>";
            default:
                throw new System.NotImplementedException();
            }
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
        { 
            var res = new Value() { type = Type.Closure, val = new DataUnion() { asClosure = val } };
            val.upvalues = new Value[val.chunk.UpvalueCount];
            return res;
        }

        public static Value New(UpvalueInternal val)
            => new Value() { type = Type.Upvalue, val = new DataUnion() { asUpvalue = val } };

        public static Value New(ClassInternal val)
            => new Value() { type = Type.Class, val = new DataUnion() { asClass = val } };

        public static Value New(InstanceInternal val)
            => new Value() { type = Type.Instance, val = new DataUnion() { asInstance = val } };

        public static Value Null()
            => new Value() { type = Type.Null };
    }
}
