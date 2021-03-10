using System.Collections.Generic;

namespace ULox
{
    public class Chunk
    {
        public List<byte> instructions = new List<byte>();
        public List<Value> constants = new List<Value>();
        public List<int> lines = new List<int>();
        public string Name { get; set; }

        public Chunk(string name)
        {
            Name = name;
        }

        public void WriteByte(byte b, int line)
        {
            instructions.Add(b);
            lines.Add(line);
        }

        public void WriteConstant(Value val, int line)
        {
            instructions.Add((byte)OpCode.CONSTANT);
            instructions.Add(AddConstant(val));
            lines.Add(line);
        }

        public void WriteSimple(OpCode opCode, int line)
        {
            instructions.Add((byte)opCode);
            lines.Add(line);
        }

        public byte AddConstant(Value val)
        {
            System.Diagnostics.Debug.Assert(constants.Count < byte.MaxValue);
            constants.Add(val);
            return (byte) (constants.Count - 1);
        }

        public Value ReadConstant(byte index) => constants[index];
    }
}
