﻿using System.Collections.Generic;

namespace ULox.ByteCode
{
    public class ByteCodeListClass : ClassInternal
    {
        private const string ListFieldName = "list";
        private class InternalList : List<Value> { }

        public ByteCodeListClass()
        {
            this.name = "List";
            this.methods.Add(VM.InitMethodName, Value.New(InitInstance));
            this.methods.Add(nameof(Count), Value.New(Count));
            this.methods.Add(nameof(Get), Value.New(Get));
            this.methods.Add(nameof(Set), Value.New(Set));
            this.methods.Add(nameof(Add), Value.New(Add));
            this.initialiser = this.methods[VM.InitMethodName];
        }

        private Value InitInstance(VM vm, int argCount)
        {
            var inst = vm.GetArg(0);
            inst.val.asInstance.fields.Add(ListFieldName, Value.Object(new InternalList()));
            return inst;
        }

        private Value Count(VM vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var list = inst.val.asInstance.fields[ListFieldName].val.asObject as InternalList;
            return Value.New(list.Count);
        }

        private Value Get(VM vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var list = inst.val.asInstance.fields[ListFieldName].val.asObject as InternalList;
            int index = (int)vm.GetArg(1).val.asDouble;
            return list[index];
        }

        private Value Set(VM vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var list = inst.val.asInstance.fields[ListFieldName].val.asObject as InternalList;
            int index = (int)vm.GetArg(1).val.asDouble;
            var newValue = vm.GetArg(2);
            list[index] = newValue;
            return newValue;
        }

        private Value Add(VM vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var list = inst.val.asInstance.fields[ListFieldName].val.asObject as InternalList;
            var newValue = vm.GetArg(1);
            list.Add(newValue);
            return newValue;
        }
    }
}
