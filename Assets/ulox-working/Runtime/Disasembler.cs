﻿using System.Collections.Generic;
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
                case OpCode.GET_LOCAL:
                case OpCode.SET_LOCAL:
                case OpCode.CLASS:
                case OpCode.CALL:
                    {
                        stringBuilder.Append(" ");
                        i++;
                        var ind = chunk.instructions[i];
                        stringBuilder.Append($"({ind})" + chunk.ReadConstant(ind).ToString());
                    }
                    break;
                case OpCode.GET_UPVALUE:
                case OpCode.SET_UPVALUE:
                    {
                        stringBuilder.Append(" ");
                        i++;
                        var ind = chunk.instructions[i];
                        stringBuilder.Append($"({ind})");
                    }
                    break;
                case OpCode.CLOSURE:
                    {
                        stringBuilder.Append(" ");
                        i++;
                        var ind = chunk.instructions[i];
                        var func = chunk.ReadConstant(ind);
                        stringBuilder.Append($"({ind})" + func.ToString());

                        if (func.val.asChunk.UpvalueCount > 0)
                            stringBuilder.AppendLine();

                        var count = func.val.asChunk.UpvalueCount;
                        for (int upVal = 0; upVal < count; upVal++)
                        {
                            i++;
                            var isLocal = chunk.instructions[i];
                            i++;
                            var upvalIndex = chunk.instructions[i];

                            stringBuilder.Append($"     {(isLocal == 1 ? "local" : "upvalue" )} {upvalIndex}");
                            if (upVal < count - 1)
                                stringBuilder.AppendLine();
                        }
                    }
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
                case OpCode.CLOSE_UPVALUE:
                default:
                    break;
                }
                stringBuilder.AppendLine();
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("--constants--");

            var subChunks = new List<Chunk>();

            for (int i = 0; i < chunk.constants.Count; i++)
            {
                var v = chunk.constants[i];

                stringBuilder.Append(i.ToString("000"));
                stringBuilder.Append("  ");
                stringBuilder.Append(v.ToString());
                stringBuilder.AppendLine();

                if(v.type == Value.Type.Chunk)
                {
                    subChunks.Add(v.val.asChunk);
                }
            }

            stringBuilder.AppendLine("####");
            stringBuilder.AppendLine();
            foreach (var c in subChunks)
            {
                DoChunk(c);
            }
        }
    }
}
