namespace ULox
{
    public class ByteCodeAssertLibrary : ILoxByteCodeLibrary
    {
        public void BindToEngine(ByteCodeInterpreterEngine engine)
        {
            var assertInst = new InstanceInternal();
            engine.VM.SetGlobal("Assert", Value.New(assertInst));

            assertInst.fields[nameof(AreEqual)] = Value.New(AreEqual);
            assertInst.fields[nameof(AreApproxEqual)] = Value.New(AreApproxEqual);
        }

        private static Value AreApproxEqual(VM vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if(lhs.type != Value.Type.Double || rhs.type != Value.Type.Double)
                throw new AssertException($"Cannot perform AreApproxEqual on non-double types, '{lhs}', '{rhs}'.");

            var dif = lhs.val.asDouble - rhs.val.asDouble;
            var squareDif = dif * dif;
            if (squareDif > 1e-16)
                throw new AssertException($"'{lhs}' and '{rhs}' are '{dif}' apart.");

            return Value.Null();
        }

        private static Value AreEqual(VM vm, int argCount)
        {
            var lhs = vm.GetArg(1);
            var rhs = vm.GetArg(2);
            if (!VM.VMValueCompare(ref lhs, ref rhs))
                throw new AssertException($"'{lhs}' does not equal '{rhs}'.");

            return Value.Null();
        }
    }

    public class ByteCodeStandardClassesLibrary : ILoxByteCodeLibrary
    {
        public void BindToEngine(ByteCodeInterpreterEngine engine)
        {
            engine.VM.SetGlobal("List", Value.New(new ByteCodeListClass()));
        }
    }
    
    public class ByteCodeCoreLibrary : ILoxByteCodeLibrary
    {
        private System.Action<string> _printer;

        public ByteCodeCoreLibrary(System.Action<string> printer) { _printer = printer; }

        public void BindToEngine(ByteCodeInterpreterEngine engine)
        {
            engine.VM.SetGlobal("print", Value.New(
                (vm, args) =>
                {
                    _printer?.Invoke(vm.GetArg(1).val.asString);
                    return Value.Null();
                }));
        }
    }

    public class UnityByteCodeLibrary : ILoxByteCodeLibrary
    {
        System.Collections.Generic.List<UnityEngine.GameObject> _availablePrefabs;

        public UnityByteCodeLibrary(System.Collections.Generic.List<UnityEngine.GameObject> availablePrefabs) 
        {
            _availablePrefabs = availablePrefabs;
        }

        public void BindToEngine(ByteCodeInterpreterEngine engine)
        {
            engine.VM.SetGlobal("CreateFromPrefab",
                 Value.New(
                (vm, args) =>
                {
                    var targetName = vm.GetArg(1).val.asString;
                    var loc = _availablePrefabs.Find(x => x.name == targetName);
                    if(loc != null)
                        return Value.Object(UnityEngine.Object.Instantiate(loc));
                    return Value.Null();
                }));

            engine.VM.SetGlobal("SetGameObjectPosition",
                 Value.New(
                (vm, args) =>
                {
                    var go = vm.GetArg(1).val.asObject as UnityEngine.GameObject;
                    float x = (float)vm.GetArg(2).val.asDouble;
                    float y = (float)vm.GetArg(3).val.asDouble;
                    float z = (float)vm.GetArg(4).val.asDouble;
                    go.transform.position = new UnityEngine.Vector3(x, y, z);
                    return Value.Null();
                }));

            engine.VM.SetGlobal("RandRange",
                 Value.New(
                (vm, args) =>
                {
                    var min = vm.GetArg(1).val.asDouble;
                    var max = vm.GetArg(2).val.asDouble;
                    return Value.New(UnityEngine.Random.Range((float)min, (float)max));
                }));

            engine.VM.SetGlobal("GetKey",
                 Value.New(
                (vm, args) =>
                {
                    var keyName = vm.GetArg(1).val.asString;
                    return Value.New(UnityEngine.Input.GetKey(keyName));
                }));
        }
    }
}
