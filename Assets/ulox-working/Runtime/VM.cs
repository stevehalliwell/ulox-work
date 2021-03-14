using System.Collections.Generic;

namespace ULox
{
    public enum InterpreterResult
    {
        OK,
        COMPILE_ERROR,
        RUNTIME_ERROR,
    }

    public class CallFrame
    {
        public Chunk chunk;
        public int ip;
        public int stackStart;
    }


    public class VM
    {
        private IndexableStack<Value> valueStack = new IndexableStack<Value>();
        private Dictionary<string, Value> globals = new Dictionary<string, Value>();
        private IndexableStack<CallFrame> callFrames = new IndexableStack<CallFrame>();
        private int CurrentCallFrame => callFrames.Count-1;

        private int CurrentIP
        {
            get { return callFrames[CurrentCallFrame].ip; }
            set { callFrames[CurrentCallFrame].ip = value; }
        }

        private System.Action<string> _printer;

        public VM(System.Action<string> printer)
        {
            _printer = printer; 
        }

        public string GenerateStackDump()
        {
            return new DumpStack().Generate(valueStack);
        }

        public string GenerateGlobalsDump()
        {
            return new DumpGlobals().Generate(globals);
        }

        public InterpreterResult Interpret(Chunk chunk)
        {
            Call(chunk, 0);

            return Run();
        }

        private InterpreterResult Run()
        {
            while (true)
            {
                var chunk = callFrames[CurrentCallFrame].chunk;

                OpCode opCode = ReadOpCode(chunk);

                switch (opCode)
                {
                case OpCode.CONSTANT:
                    var constantIndex = ReadByte(chunk);
                    valueStack.Push(chunk.ReadConstant(constantIndex));
                    break;
                case OpCode.RETURN:
                    {
                        Value result = valueStack.Pop();

                        var prev = callFrames.Pop();
                        if (callFrames.Count == 0)
                        {
                            //valueStack.Pop(); Expects a self func ref on stack that we aren't doing yet
                            return InterpreterResult.OK;
                        }

                        while (valueStack.Count > prev.stackStart)
                            valueStack.Pop();

                        valueStack.Push(result);
                    }
                    break;
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
                            CurrentIP += jump;
                    }
                    break;
                case OpCode.JUMP:
                    {
                        ushort jump = ReadUShort(chunk);
                        CurrentIP += jump;
                    }
                    break;
                case OpCode.LOOP:
                    {
                        ushort jump = ReadUShort(chunk);
                        CurrentIP -= jump;
                    }
                    break;
                case OpCode.FETCH_LOCAL:
                    {
                        var slot = ReadByte(chunk);
                        valueStack.Push(FetchLocalStack(slot));
                    }
                    break;
                case OpCode.ASSIGN_LOCAL:
                    {
                        var slot = ReadByte(chunk);
                        AssignLocalStack(slot, valueStack.Peek());
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
                case OpCode.CALL:
                    {
                        int argCount = ReadByte(chunk);
                        if (!CallValue(valueStack.Peek(argCount), argCount))
                        {
                            return InterpreterResult.RUNTIME_ERROR;
                        }
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

        private bool CallValue(Value callee, int argCount)
        {
            if (callee.type == Value.Type.Function)
            {
                return Call(callee.val.asChunk, argCount);
            }

            throw new VMException("Can only call functions and classes.");
        }

        private bool Call(Chunk asChunk, int argCount)
        {
            if (argCount != asChunk.Arity)
                throw new VMException($"Wrong number of params given to '{asChunk.Name}'" +
                    $", got '{argCount}' but expected '{asChunk.Arity}'");

            callFrames.Push(new CallFrame()
            {
                chunk = asChunk,
                stackStart = valueStack.Count - argCount
            });
            return true;
        }

        private void AssignLocalStack(byte slot, Value val)
        {
            valueStack[slot + callFrames[CurrentCallFrame].stackStart] = val;
        }

        private Value FetchLocalStack(byte slot)
        {
            return valueStack[slot + callFrames[CurrentCallFrame].stackStart];
        }

        private OpCode ReadOpCode(Chunk chunk)
        {
            var opCode = (OpCode)chunk.instructions[CurrentIP];
            CurrentIP++;
            return opCode;
        }

        private byte ReadByte(Chunk chunk)
        {
            var b = chunk.instructions[CurrentIP];
            CurrentIP++;
            return b;
        }

        private ushort ReadUShort(Chunk chunk)
        {
            var bhi = chunk.instructions[CurrentIP];
            CurrentIP++;
            var blo = chunk.instructions[CurrentIP];
            CurrentIP++;
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
