using System.Collections.Generic;

namespace ULox
{
    public enum InterpreterResult
    {
        OK,
        COMPILE_ERROR,
        RUNTIME_ERROR,
    }

    public class VM
    {
        private int ip;
        private IndexableStack<Value> valueStack = new IndexableStack<Value>();
        private Dictionary<string, Value> globals = new Dictionary<string, Value>();

        private System.Action<string> _printer;

        public VM(System.Action<string> printer)
        {
            _printer = printer;
        }

        public string GenerateStackDump()
        {
            return new DumpStack().Generate(valueStack);
        }

        public InterpreterResult Interpret(Chunk chunk)
        {
            while(true)
            {
                OpCode opCode = ReadOpCode(chunk);

                switch (opCode)
                {
                case OpCode.CONSTANT:
                    var constantIndex = ReadByte(chunk);
                    valueStack.Push(chunk.ReadConstant(constantIndex));
                    break;
                case OpCode.RETURN:
                    return InterpreterResult.OK;
                case OpCode.NEGATE:
                    valueStack.Push(Value.New(-valueStack.Pop().val.asDouble));
                    break;
                case OpCode.ADD:
                case OpCode.SUBTRACT:
                case OpCode.MULTIPLY:
                case OpCode.DIVIDE:
                    DoMathOp(opCode);
                    break;
                case OpCode.EQUAL:
                case OpCode.LESS:
                case OpCode.GREATER:
                    DoComparisonOp(opCode);
                    break;
                case OpCode.NOT:
                    valueStack.Push(Value.New(valueStack.Pop().IsFalsey));
                    break;
                case OpCode.TRUE:
                    valueStack.Push(Value.New(true));
                    break;
                case OpCode.FALSE:
                    valueStack.Push(Value.New(false));
                    break;
                case OpCode.NULL:
                    valueStack.Push(Value.Null());
                    break;
                case OpCode.PRINT:
                    _printer(valueStack.Pop().ToString());
                    break;
                case OpCode.POP:
                    _ = valueStack.Pop();
                    break;
                case OpCode.JUMP_IF_FALSE:
                    {
                        ushort jump = ReadUShort(chunk);
                        if (valueStack.Peek().IsFalsey)
                            ip += jump;
                    }
                    break;
                case OpCode.JUMP:
                    {
                        ushort jump = ReadUShort(chunk);
                        ip += jump;
                    }
                    break;
                case OpCode.LOOP:
                    {
                        ushort jump = ReadUShort(chunk);
                        ip -= jump;
                    }
                    break;
                case OpCode.FETCH_LOCAL:
                    {
                        var slot = ReadByte(chunk);
                        valueStack.Push(valueStack[slot]);
                    }
                    break;
                case OpCode.ASSIGN_LOCAL:
                    {
                        var slot = ReadByte(chunk);
                        valueStack[slot] = valueStack.Peek();
                    }
                    break;
                case OpCode.DEFINE_GLOBAL:
                    {
                        var global = ReadByte(chunk);
                        var globalName = chunk.ReadConstant(global);
                        globals[globalName.val.asString] = valueStack.Pop();
                    }
                    break;
                case OpCode.FETCH_GLOBAL:
                    {
                        var global = ReadByte(chunk);
                        var globalName = chunk.ReadConstant(global);
                        var actualName = globalName.val.asString;
                        if (!globals.TryGetValue(actualName, out var globalValue))
                        {
                            throw new VMException($"Global var of name '{actualName}' was not found.");
                        }
                        valueStack.Push(globalValue);
                    }
                    break;
                case OpCode.ASSIGN_GLOBAL:
                    {
                        var global = ReadByte(chunk);
                        var globalName = chunk.ReadConstant(global);
                        var actualName = globalName.val.asString;
                        if (!globals.ContainsKey(actualName))
                        {
                            throw new VMException($"Global var of name '{actualName}' was not found.");
                        }
                        globals[actualName] = valueStack.Peek();
                    }
                    break;
                case OpCode.NONE:
                    break;
                default:
                    break;
                }
            }

            return InterpreterResult.OK;
        }

        private OpCode ReadOpCode(Chunk chunk)
        {
            var opCode = (OpCode)chunk.instructions[ip];
            ip++;
            return opCode;
        }

        private byte ReadByte(Chunk chunk)
        {
            var b = chunk.instructions[ip];
            ip++;
            return b;
        }

        private ushort ReadUShort(Chunk chunk)
        {
            var bhi = chunk.instructions[ip];
            ip++;
            var blo = chunk.instructions[ip];
            ip++;
            return (ushort)((bhi << 8) | blo);
        }

        private void DoMathOp(OpCode opCode)
        {
            var rhs = valueStack.Pop();
            var lhs = valueStack.Pop();

            if(opCode == OpCode.ADD && lhs.type == Value.Type.String && rhs.type == lhs.type)
            {
                valueStack.Push(Value.New(lhs.val.asString + rhs.val.asString));
                return;
            }

            if (lhs.type != Value.Type.Double && lhs.type != rhs.type)
                throw new VMException($"Cannot perform math op on non math types '{lhs.type}' and '{rhs.type}'.");

            var res = Value.New(0);
            switch (opCode)
            {
            case OpCode.ADD:
                res.val.asDouble = lhs.val.asDouble + rhs.val.asDouble;
                break;
            case OpCode.SUBTRACT:
                res.val.asDouble = lhs.val.asDouble - rhs.val.asDouble;
                break;
            case OpCode.MULTIPLY:
                res.val.asDouble = lhs.val.asDouble * rhs.val.asDouble;
                break;
            case OpCode.DIVIDE:
                res.val.asDouble = lhs.val.asDouble / rhs.val.asDouble;
                break;
            }
            valueStack.Push(res);
        }

        private void DoComparisonOp(OpCode opCode)
        {
            var rhs = valueStack.Pop();
            var lhs = valueStack.Pop();
            //todo fix handling of NaNs on either side
            switch (opCode)
            {
            case OpCode.EQUAL:
                if (lhs.type != rhs.type)
                {
                    valueStack.Push(Value.New(false));
                    return;
                }
                else
                {
                    switch (lhs.type)
                    {
                    case Value.Type.Null:
                        valueStack.Push(Value.New(true));
                        break;
                    case Value.Type.Double:
                        valueStack.Push(Value.New(lhs.val.asDouble == rhs.val.asDouble));
                        break;
                    case Value.Type.Bool:
                        valueStack.Push(Value.New(lhs.val.asBool == rhs.val.asBool));
                        break;
                    case Value.Type.String:
                        valueStack.Push(Value.New(lhs.val.asString == rhs.val.asString));
                        break;
                    default:
                        break;
                    }
                }
                break;
            case OpCode.LESS:
                if (lhs.type != Value.Type.Double || lhs.type != rhs.type)
                    throw new VMException($"Cannot less compare on different types '{lhs.type}' and '{rhs.type}'.");
                valueStack.Push(Value.New(lhs.val.asDouble < rhs.val.asDouble));
                break;
            case OpCode.GREATER:
                if (lhs.type != Value.Type.Double || lhs.type != rhs.type)
                    throw new VMException($"Cannot greater across on different types '{lhs.type}' and '{rhs.type}'.");
                valueStack.Push(Value.New(lhs.val.asDouble > rhs.val.asDouble));
                break;
            }
        }
    }
}
