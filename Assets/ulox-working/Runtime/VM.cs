﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    //todo ability to ask if field or method exists at runtime
    //todo ability to add remove fields and methods at runtime?
    //todo introduce optimiser, pass after compile, have compile build slim ast as it goes
    //  convert labels to offsets
    //  identify and remove unused constants
    //todo better string parsing token support
    //todo add conditional
    //todo add self assignment
    //todo add pods
    //todo add more to libraries
    //todo add sandboxing
    //todo auto init assign
    //todo add classof
    //todo multiple returns?
    //todo add operator overloads
    //todo add testing instructions
    //todo emit functions when no upvals are required https://github.com/munificent/craftinginterpreters/blob/master/note/answers/chapter25_closures/1.md
    //todo better, standardisead errors, including from native
    public enum InterpreterResult
    {
        OK,
        COMPILE_ERROR,
        RUNTIME_ERROR,
    }

    public struct CallFrame
    {
        public int ip;
        public int stackStart;
        public ClosureInternal closure;
    }

    public class Table : Dictionary<string, Value> { }

    public class VM
    {
        public const string InitMethodName = "init";
        private FastStack<Value> _valueStack = new FastStack<Value>();

        private void Push(Value val) => _valueStack.Push(val);
        //todo how many of these actually need to be pop vs discard pop
        private Value Pop() => _valueStack.Pop();
        private Value Peek(int ind = 0) => _valueStack.Peek(ind);
        private FastStack<CallFrame> _callFrames = new FastStack<CallFrame>();
        private CallFrame currentCallFrame;
        private LinkedList<Value> openUpvalues = new LinkedList<Value>();
        private IndexedTable globals = new IndexedTable();

        public void SetGlobal(string name, Value val)
        {
            globals[name] = val;
        }

        public Value GetGlobal(string name)
        {
            return globals[name];
        }

        public Value GetArg(int index)
        {
            return _valueStack[currentCallFrame.stackStart+index];
        }

        public InterpreterResult CallFunction(Value func, int args)
        {
            CallValue(func, args);
            return Run();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte ReadByte(Chunk chunk)
        {
            var b = chunk.instructions[currentCallFrame.ip];
            currentCallFrame.ip++;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort ReadUShort(Chunk chunk)
        {
            var bhi = chunk.instructions[currentCallFrame.ip];
            currentCallFrame.ip++;
            var blo = chunk.instructions[currentCallFrame.ip];
            currentCallFrame.ip++;
            return (ushort)((bhi << 8) | blo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushNewCallframe(CallFrame callFrame)
        {
            if (_callFrames.Count > 0)
            {
                //save current state
                _callFrames.SetAt(_callFrames.Count - 1, currentCallFrame);
            }

            currentCallFrame = callFrame;
            _callFrames.Push(callFrame);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PopCallFrame()
        {
            //remove top
            _callFrames.Pop();

            //update cache
            if (_callFrames.Count > 0)
                currentCallFrame = _callFrames.Peek();
            else
                currentCallFrame = default;
        }

        public string GenerateStackDump()
        {
            return new DumpStack().Generate(_valueStack);
        }

        public string GenerateGlobalsDump()
        {
            return new DumpGlobals().Generate(globals);
        }

        public InterpreterResult Interpret(Chunk chunk)
        {
            Push(Value.New(""));
            Push(Value.New(new ClosureInternal() { chunk = chunk }));
            CallValue(Peek(), 0);

            return Run();
        }

        private InterpreterResult Run()
        {
            while (true)
            {
                var chunk = currentCallFrame.closure.chunk;

                OpCode opCode = (OpCode)ReadByte(chunk);

                switch (opCode)
                {
                case OpCode.CONSTANT:
                    {
                        var constantIndex = ReadByte(chunk);
                        Push(chunk.ReadConstant(constantIndex));
                    }
                    break;
                case OpCode.RETURN:
                    {
                        Value result = Pop();

                        CloseUpvalues(currentCallFrame.stackStart);

                        var prevStackStart = currentCallFrame.stackStart;

                        PopCallFrame();
                        
                        if (_callFrames.Count == 0)
                        {
                            Pop();
                            return InterpreterResult.OK;
                        }

                         _valueStack.DiscardPop(_valueStack.Count - prevStackStart);

                        Push(result);
                    }
                    break;
                case OpCode.NEGATE:
                    Push(Value.New(-Pop().val.asDouble));
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
                    Push(Value.New(Pop().IsFalsey));
                    break;
                case OpCode.PUSH_BOOL:
                    {
                        var b = ReadByte(chunk);
                        if (b == 1)
                            Push(Value.New(true));
                        else
                            Push(Value.New(false));
                    }
                    break;
                case OpCode.NULL:
                    Push(Value.Null());
                    break;
                case OpCode.PUSH_BYTE:
                    {
                        var b = ReadByte(chunk);
                        Push(Value.New(b));
                    }
                    break;
                case OpCode.POP:
                    _ = Pop();
                    break;
                case OpCode.JUMP_IF_FALSE:
                    {
                        ushort jump = ReadUShort(chunk);
                        if (Peek().IsFalsey)
                            currentCallFrame.ip += jump;
                    }
                    break;
                case OpCode.JUMP:
                    {
                        ushort jump = ReadUShort(chunk);
                        currentCallFrame.ip += jump;
                    }
                    break;
                case OpCode.LOOP:
                    {
                        ushort jump = ReadUShort(chunk);
                        currentCallFrame.ip -= jump;
                    }
                    break;
                case OpCode.GET_LOCAL:
                    {
                        var slot = ReadByte(chunk);
                        Push(_valueStack[currentCallFrame.stackStart + slot]);
                    }
                    break;
                case OpCode.SET_LOCAL:
                    {
                        var slot = ReadByte(chunk);
                        _valueStack[currentCallFrame.stackStart + slot] = Peek();
                    }
                    break;
                case OpCode.GET_UPVALUE:
                    {
                        var slot = ReadByte(chunk);
                        var upval = currentCallFrame.closure.upvalues[slot].val.asUpvalue;
                        if (!upval.isClosed)
                             Push(_valueStack[upval.index]);
                        else
                            Push(upval.value);
                    }
                    break;
                case OpCode.SET_UPVALUE:
                    {
                        var slot = ReadByte(chunk);
                        var upval = currentCallFrame.closure.upvalues[slot].val.asUpvalue;
                        if (!upval.isClosed)
                            _valueStack[upval.index] = Peek();
                        else
                            upval.value = Peek();
                    }
                    break;
                case OpCode.DEFINE_GLOBAL:
                    {
                        var global = ReadByte(chunk);
                        var globalName = chunk.ReadConstant(global);
                        globals[globalName.val.asString] = Pop();
                    }
                    break;
                case OpCode.FETCH_GLOBAL_UNCACHED:
                    {
                        var global = ReadByte(chunk);
                        var globalName = chunk.ReadConstant(global);
                        var actualName = globalName.val.asString;
                        var index = globals.FindIndex(actualName);
                        if (index == -1)
                        {
                            throw new VMException($"Global var of name '{actualName}' was not found.");
                        }
                        Push(globals[index]);

                        //it worked patch the instruction so we never have to be in here again.
                        chunk.instructions[currentCallFrame.ip - 2] = (byte)OpCode.FETCH_GLOBAL_CACHED;
                        chunk.instructions[currentCallFrame.ip - 1] = (byte)index;
                    }
                    break;
                case OpCode.FETCH_GLOBAL_CACHED:
                    {
                        var index = ReadByte(chunk);
                        Push(globals[index]);
                    }
                    break;
                case OpCode.ASSIGN_GLOBAL_UNCACHED:
                    {
                        var global = ReadByte(chunk);
                        var globalName = chunk.ReadConstant(global);
                        var actualName = globalName.val.asString;
                        var index = globals.FindIndex(actualName);
                        if (index == -1)
                        {
                            throw new VMException($"Global var of name '{actualName}' was not found.");
                        }
                        globals[index] = Peek();

                        //it worked patch the instruction so we never have to be in here again.
                        chunk.instructions[currentCallFrame.ip - 2] = (byte)OpCode.ASSIGN_GLOBAL_CACHED;
                        chunk.instructions[currentCallFrame.ip - 1] = (byte)index;
                    }
                    break;
                case OpCode.ASSIGN_GLOBAL_CACHED:
                    {
                        var index = ReadByte(chunk);
                        globals[index] = Peek();
                    }
                    break;
                case OpCode.CALL:
                    {
                        int argCount = ReadByte(chunk);
                        if (!CallValue(Peek(argCount), argCount))
                        {
                            return InterpreterResult.RUNTIME_ERROR;
                        }
                    }
                    break;
                case OpCode.INVOKE_UNCACHED:
                    {
                        var constantIndex = ReadByte(chunk);
                        var methodName = chunk.ReadConstant(constantIndex).val.asString;
                        var argCount = ReadByte(chunk);

                        var receiver = Peek(argCount);
                        if (receiver.type != Value.Type.Instance)
                            throw new VMException("Cannot invoke on non instance receiver.");

                        var inst = receiver.val.asInstance;

                        //it could be a field
                        if (inst.fields.TryGetValue(methodName, out var fieldFunc))
                        {
                            _valueStack[_valueStack.Count - 1 - argCount] = fieldFunc;
                            if (!CallValue(fieldFunc, argCount))
                            {
                                return InterpreterResult.RUNTIME_ERROR;
                            }
                        }
                        else
                        {
                            //it must be a method, if it is we can rewrite
                            var fromClass = inst.fromClass;
                            var index = fromClass.methods.FindIndex(methodName);
                            if (index == -1)
                            {
                                throw new VMException($"No method of name '{methodName}' found on '{fromClass}'.");
                            }

                            var method = fromClass.methods[index];

                            //it worked so lets rewrite, it's a 3 byte instructions, the last being the arg count
                            chunk.instructions[currentCallFrame.ip - 3] = (byte)OpCode.INVOKE_CACHED;
                            chunk.instructions[currentCallFrame.ip - 2] = (byte)index;

                            if (!CallValue(method, argCount))
                                return InterpreterResult.RUNTIME_ERROR;
                        }
                    }
                    break;
                case OpCode.INVOKE_CACHED:
                    {
                        var index = ReadByte(chunk);
                        var argCount = ReadByte(chunk);
                        var receiver = Peek(argCount);
                        var inst = receiver.val.asInstance;
                        var fromClass = inst.fromClass;
                        var method = fromClass.methods[index];
                        if (!CallValue(method, argCount))
                            return InterpreterResult.RUNTIME_ERROR;
                    }
                    break;
                case OpCode.CLOSURE:
                    {
                        var constantIndex = ReadByte(chunk);
                        var func = chunk.ReadConstant(constantIndex);
                        var closureVal = Value.New(new ClosureInternal() { chunk = func.val.asChunk });
                        Push(closureVal);

                        var closure = closureVal.val.asClosure;

                        for (int i = 0; i < closure.upvalues.Length; i++)
                        {
                            var isLocal = ReadByte(chunk);
                            var index = ReadByte(chunk);
                            if(isLocal == 1)
                            {
                                var local = currentCallFrame.stackStart + index;
                                closure.upvalues[i] = CaptureUpvalue(local);
                            }
                            else
                            {
                                closure.upvalues[i] = currentCallFrame.closure.upvalues[index];
                            }
                        }
                    }
                    break;
                case OpCode.CLOSE_UPVALUE:
                    CloseUpvalues(_valueStack.Count-1);
                    Pop();
                    break;
                case OpCode.CLASS:
                    {
                        var constantIndex = ReadByte(chunk);
                        var name = chunk.ReadConstant(constantIndex);
                        Push(Value.New( new ClassInternal() { name = name.val.asString }));
                    }
                    break;
                case OpCode.GET_PROPERTY_UNCACHED:
                    {
                        //use class to build a cached route to the field, introduce an cannot cache instruction
                        //  once there are class vars this can be done through that as those are known and safe, not
                        //  dynamically added at will to arbitrary objects.
                        //if the class has the property name then we know it MUST be there, find it's index and then rewrite
                        //  problem then is that the chunk could be given different object types, we need to fall back if a
                        //  different type is given or generate variants for each type
                        //the problem here is we don't know that the targetVal is of the same type that we are caching so 
                        //  turn it off for now.
                        var targetVal = Peek();
                        if (targetVal.type != Value.Type.Instance)
                            throw new VMException($"Only instances have properties. Got {targetVal}.");
                        //todo add inline caching of some kind
                        var instance = targetVal.val.asInstance;
                        var constantIndex = ReadByte(chunk);
                        var name = chunk.ReadConstant(constantIndex).val.asString;


                        if (instance.fields.TryGetValue(name, out var val))
                        {
                            Pop();
                            Push(val);
                            break;
                        }

                        if(!BindMethod(instance.fromClass, name))
                        {
                            return InterpreterResult.RUNTIME_ERROR;
                        }
                    }
                    break;
                case OpCode.SET_PROPERTY_UNCACHED:
                    {
                        var targetVal = Peek(1);
                        if (targetVal.type != Value.Type.Instance)
                            throw new VMException($"Only instances have properties. Got {targetVal}.");
                        //todo add inline caching of some kind
                        var instance = targetVal.val.asInstance;

                        var constantIndex = ReadByte(chunk);
                        var name = chunk.ReadConstant(constantIndex).val.asString;
                        
                        instance.fields[name] = Peek();

                        var value = Pop();
                        Pop();
                        Push(value);
                    }
                    break;
                case OpCode.METHOD:
                    {
                        var constantIndex = ReadByte(chunk);
                        var name = chunk.ReadConstant(constantIndex).val.asString;
                        DefineMethod(name);
                    }
                    break;
                case OpCode.PROPERTY:
                    {
                        var constantIndex = ReadByte(chunk);
                        var name = chunk.ReadConstant(constantIndex).val.asString;
                        var klass = Peek().val.asClass;
                        klass.properties.Add(name);
                    }
                    break;
                case OpCode.INHERIT:
                    {
                        var superClass = Peek(1);
                        if (superClass.type != Value.Type.Class)
                            throw new VMException("Superclass must be a class.");

                        var subClass = Peek();
                        var subMethods = subClass.val.asClass.methods;
                        var superMethods = superClass.val.asClass.methods;
                        foreach (var item in superMethods)
                        {
                            var k = item.Key;
                            var v = superMethods[item.Value];
                            subMethods.Add(k, v);
                        }

                        Pop();
                    }
                    break;
                case OpCode.GET_SUPER:
                    {
                        var constantIndex = ReadByte(chunk);
                        var name = chunk.ReadConstant(constantIndex).val.asString;
                        var superClassVal = Pop();
                        var superClass = superClassVal.val.asClass;
                        if (!BindMethod(superClass, name))
                            return InterpreterResult.RUNTIME_ERROR;
                    }
                    break;
                case OpCode.SUPER_INVOKE:
                    {
                        UnityEngine.Debug.Log(GenerateStackDump());
                        var constantIndex = ReadByte(chunk);
                        var methName = chunk.ReadConstant(constantIndex).val.asString;
                        var argCount = ReadByte(chunk);
                        var superClass = Pop().val.asClass;
                        if (!InvokeFromClass(superClass, methName, argCount))
                        {
                            return InterpreterResult.RUNTIME_ERROR;
                        }
                    }
                    break;
                case OpCode.THROW:
                    {
                        var value = Pop();
                        throw new PanicException(value.ToString());
                    }
                case OpCode.NONE:
                    break;
                default:
                    break;
                }
            }

            return InterpreterResult.OK;
        }

        private bool BindMethod(ClassInternal fromClass, string name)
        {
            var index = fromClass.methods.FindIndex(name);
            if (index == -1)
            {
                throw new VMException($"Undefined property {name}");
            }
            var value = fromClass.methods[index];

            var bound = Value.New(new BoundMethod() { receiver = Peek(), method = value.val.asClosure });

            Pop();
            Push(bound);
            return true;
        }

        private void DefineMethod(string name)
        {
            Value method = Peek();
            var klass = Peek(1).val.asClass;
            klass.methods[name] = method;
            if(name == InitMethodName)
            {
                klass.initialiser = method;
            }
            Pop();
        }

        private void CloseUpvalues(int last)
        {
            while (openUpvalues.Count > 0 &&
                openUpvalues.First.Value.val.asUpvalue.index >= last)
            {
                var upvalue = openUpvalues.First.Value.val.asUpvalue;
                upvalue.value = _valueStack[upvalue.index];
                upvalue.index = -1;
                upvalue.isClosed = true;
                openUpvalues.RemoveFirst();
            }
        }

        private Value CaptureUpvalue(int index)
        {
            var node = openUpvalues.First;

            while (node != null && node.Value.val.asUpvalue.index > index)
            {
                node = node.Next;
            }

            if (node != null && node.Value.val.asUpvalue.index == index)
            {
                return node.Value;
            }

            var upvalIn = new UpvalueInternal() {index = index };
            var upval = Value.New(upvalIn);

            if (node != null)
                openUpvalues.AddBefore(node, upval);
            else
                openUpvalues.AddLast(upval);

            return upval;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CallValue(Value callee, int argCount)
        {
            switch (callee.type)
            {
            case Value.Type.NativeFunction: return CallNative(callee.val.asNativeFunc, argCount);
            case Value.Type.Closure: return Call(callee.val.asClosure, argCount);
            case Value.Type.Class: return CreateInstance(callee.val.asClass, argCount);
            case Value.Type.BoundMethod: return CallMethod(callee.val.asBoundMethod, argCount);
            }

            throw new VMException("Can only call functions and classes.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CallMethod(BoundMethod asBoundMethod, int argCount)
        {
            _valueStack[_valueStack.Count - 1 - argCount] = asBoundMethod.receiver;
            return Call(asBoundMethod.method, argCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool InvokeFromClass(ClassInternal fromClass, string methodName, int argCount)
        {
            var index = fromClass.methods.FindIndex(methodName);
            if (index == -1)
            {
                throw new VMException($"No method of name '{methodName}' found on '{fromClass}'.");
            }

            var method = fromClass.methods[index];

            return CallValue(method, argCount);
        }

        private bool CreateInstance(ClassInternal asClass, int argCount)
        {
            var instInternal = new InstanceInternal() { fromClass = asClass };
            var inst = Value.New(instInternal);
            _valueStack[_valueStack.Count - 1 - argCount] = inst;

            foreach (var item in asClass.properties)
            {
                instInternal.fields.Add(item, Value.Null());
            }

            if (!asClass.initialiser.IsNull)
            {
                //with an init list we don't return this
                return CallValue(asClass.initialiser, argCount);
            }
            else if (argCount != 0)
            {
                throw new VMException("Args given for a class that does not have an 'init' method");
            }

            //manually add this to stack and push a call frame directing the ip to the start of the init frag chain
            //return should remove the null and the this
            
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Call(ClosureInternal closureInternal, int argCount)
        {
            if (argCount != closureInternal.chunk.Arity)
                throw new VMException($"Wrong number of params given to '{closureInternal.chunk.Name}'" +
                    $", got '{argCount}' but expected '{closureInternal.chunk.Arity}'");

            PushNewCallframe(new CallFrame()
            {
                stackStart = _valueStack.Count - argCount-1,
                closure = closureInternal
            });
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CallNative(System.Func<VM, int, Value> asNativeFunc, int argCount)
        {
            PushNewCallframe(new CallFrame()
            {
                stackStart = _valueStack.Count - argCount - 1,
                closure = null
            });

            var stackPos = _valueStack.Count - argCount;
            var res = asNativeFunc.Invoke(this, argCount);

            while (_valueStack.Count > stackPos-1)
                Pop();

            PopCallFrame();

            Push(res);

            return true;
        }

        private void DoMathOp(OpCode opCode)
        {
            var rhs = Pop();
            var lhs = Pop();

            if (lhs.type != rhs.type)
                throw new VMException($"Cannot perform math op across types '{lhs.type}' and '{rhs.type}'.");

            if(opCode == OpCode.ADD && lhs.type == Value.Type.String && rhs.type == lhs.type)
            {
                Push(Value.New(lhs.val.asString + rhs.val.asString));
                return;
            }

            if(lhs.type == Value.Type.Instance)
            {
                //identify if lhs has a matching method or field
            }

            if (lhs.type != Value.Type.Double)
            {
                throw new VMException($"Cannot perform math op on non math types '{lhs.type}' and '{rhs.type}'.");
            }

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
            Push(res);
        }

        private void DoComparisonOp(OpCode opCode)
        {
            var rhs = Pop();
            var lhs = Pop();
            //todo fix handling of NaNs on either side
            switch (opCode)
            {
            case OpCode.EQUAL:
                Push(Value.New(VMValueCompare(ref lhs, ref rhs)));
                break;
            case OpCode.LESS:
                if (lhs.type != Value.Type.Double || rhs.type != Value.Type.Double)
                    throw new VMException($"Cannot less compare on different types '{lhs.type}' and '{rhs.type}'.");
                Push(Value.New(lhs.val.asDouble < rhs.val.asDouble));
                break;
            case OpCode.GREATER:
                if (lhs.type != Value.Type.Double || rhs.type != Value.Type.Double)
                    throw new VMException($"Cannot greater across on different types '{lhs.type}' and '{rhs.type}'.");
                Push(Value.New(lhs.val.asDouble > rhs.val.asDouble));
                break;
            }
        }

        public static bool VMValueCompare(ref Value lhs, ref Value rhs)
        {
            if (lhs.type != rhs.type)
            {
                return false;
            }
            else
            {
                switch (lhs.type)
                {
                case Value.Type.Null:
                    return true;
                case Value.Type.Double:
                    return lhs.val.asDouble == rhs.val.asDouble;
                case Value.Type.Bool:
                    return lhs.val.asBool == rhs.val.asBool;
                case Value.Type.String:
                    return lhs.val.asString == rhs.val.asString;
                default:
                    throw new VMException($"Cannot perform compare on type '{lhs.type}'.");
                }
            }
        }
    }
}
