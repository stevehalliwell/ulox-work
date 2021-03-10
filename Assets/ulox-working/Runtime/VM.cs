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
        private Stack<Value> valueStack = new Stack<Value>();

        public string GenerateStackDump()
        {
            return new DumpStack().Generate(valueStack);
        }

        public InterpreterResult Interpret(Chunk chunk)
        {
            while(true)
            {
                var opCode = (OpCode)chunk.instructions[ip];
                ip++;

                switch (opCode)
                {
                case OpCode.CONSTANT:
                    var index = chunk.instructions[ip];
                    ip++;
                    valueStack.Push(chunk.ReadConstant(index));
                    break;
                case OpCode.RETURN:
                    break;// return InterpreterResult.OK;
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
                    UnityEngine.Debug.Log(valueStack.Pop().ToString());
                    break;
                case OpCode.NONE:
                    break;
                default:
                    break;
                }
            }

            return InterpreterResult.OK;
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

            System.Diagnostics.Debug.Assert(lhs.type == Value.Type.Double && lhs.type == rhs.type);
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
