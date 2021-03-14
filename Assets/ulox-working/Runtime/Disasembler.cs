using System.Text;

namespace ULox
{
    public class Disasembler
    {
        private StringBuilder stringBuilder = new StringBuilder();

        public string GetString() => stringBuilder.ToString();

        public void DoChunk(Chunk chunk)
        {
            stringBuilder.AppendLine(chunk.Name);
            var instructionCount = 0;

            for (int i = 0; i < chunk.instructions.Count; i++, instructionCount++)
            {
                //todo instruction count is wrong
                stringBuilder.Append(i.ToString("0000"));

                var opCode = (OpCode)chunk.instructions[i];

                if (instructionCount == 0 ||
                    chunk.lines[instructionCount] != chunk.lines[instructionCount - 1])
                {
                    stringBuilder.Append($" {chunk.lines[instructionCount]} ");
                }
                else
                {
                    stringBuilder.Append($" | ");
                }

                stringBuilder.Append(opCode.ToString());

                switch (opCode)
                {
                case OpCode.JUMP_IF_FALSE:
                case OpCode.JUMP:
                case OpCode.LOOP:
                    stringBuilder.Append(" ");
                    i++;
                    var bhi = chunk.instructions[i];
                    i++;
                    var blo = chunk.instructions[i];
                    stringBuilder.Append((ushort)((bhi << 8) | blo));
                    break;
                case OpCode.CONSTANT:
                case OpCode.DEFINE_GLOBAL:
                case OpCode.FETCH_GLOBAL:
                case OpCode.ASSIGN_GLOBAL:
                case OpCode.FETCH_LOCAL:
                case OpCode.ASSIGN_LOCAL:
                case OpCode.CALL:
                    stringBuilder.Append(" ");
                    i++;
                    var ind = chunk.instructions[i];
                    stringBuilder.Append($"({ind})" + chunk.ReadConstant(ind).ToString());
                    break;
                case OpCode.RETURN:
                case OpCode.NEGATE:
                case OpCode.ADD:
                case OpCode.SUBTRACT:
                case OpCode.MULTIPLY:
                case OpCode.DIVIDE:
                case OpCode.NONE:
                case OpCode.NULL:
                case OpCode.TRUE:
                case OpCode.FALSE:
                case OpCode.NOT:
                case OpCode.GREATER:
                case OpCode.LESS:
                case OpCode.EQUAL:
                case OpCode.PRINT:
                case OpCode.POP:
                default:
                    break;
                }
                stringBuilder.AppendLine();
            }
        }
    }
}
