using System.Collections.Generic;

namespace ULox
{
    public class Chunk
    {
        public List<byte> instructions = new List<byte>();
        public List<Value> constants = new List<Value>();
        public List<int> lines = new List<int>();
        public string Name { get; set; }
        public int Arity { get; set; }
        public int UpvalueCount { get; internal set; }

        public Chunk(string name)
        {
            Name = name;
        }

        public void WriteByte(byte b, int line)
        {
            instructions.Add(b);
            lines.Add(line);
        }

        public byte WriteConstant(Value val, int line)
        {
            instructions.Add((byte)OpCode.CONSTANT);
            var at = AddConstant(val);
            instructions.Add(at);
            lines.Add(line);
            return at;
        }

        public void WriteSimple(OpCode opCode, int line)
        {
            instructions.Add((byte)opCode);
            lines.Add(line);
        }

        public byte AddConstant(Value val)
        {
            if (constants.Count >= byte.MaxValue)
                throw new CompilerException($"Cannot have more than '{byte.MaxValue}' constants per chunk.");

            constants.Add(val);
            return (byte) (constants.Count - 1);
        }

        public Value ReadConstant(byte index) => constants[index];
    }
}
