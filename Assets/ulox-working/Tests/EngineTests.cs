using NUnit.Framework;
using System.Linq;

namespace ULox.Tests
{

    public class EngineTests
    {
        [Test]
        public void ExternalString_EngineRead_Matches()
        {
            var test = new TestLoxEngine();
            string extVar = "test";

            test.loxEngine.SetValue(nameof(extVar), extVar);

            test.Run(@"print extVar or """";", true);

            Assert.AreEqual(extVar.ToString(), test.InterpreterResult);
        }

        [Test]
        public void ExternalBool_EngineRead_Matches()
        {
            var test = new TestLoxEngine();
            bool extVar = true;

            test.loxEngine.SetValue(nameof(extVar), extVar);

            test.Run(@"print extVar or """";", true);

            Assert.AreEqual(extVar.ToString(), test.InterpreterResult);
        }

        [Test]
        public void ExternalInt_EngineRead_Matches()
        {
            var test = new TestLoxEngine();
            int extVar = 7;

            test.loxEngine.SetValue(nameof(extVar), extVar);

            test.Run(@"print extVar or """";", true);

            Assert.AreEqual(extVar.ToString(), test.InterpreterResult);
        }

        [Test]
        public void ExternalFloat_EngineRead_Matches()
        {
            var test = new TestLoxEngine();
            float extVar = 3.14f;

            test.loxEngine.SetValue(nameof(extVar), extVar);

            test.Run(@"print extVar or """";", true);

            Assert.AreEqual(extVar.ToString().Substring(0, 4), test.InterpreterResult.Substring(0, 4));
        }

        [Test]
        public void ExternalUpdateValue_EngineRead_Matches()
        {
            var test = new TestLoxEngine();
            var initialTestString = 
@"var v = true;
print v;";

            test.Run(initialTestString, true);

            test.loxEngine.SetValue("v", false);

            test.Run("print v;", true);

            Assert.AreEqual("TrueFalse", test.InterpreterResult);
        }

        [Test]
        public void EngineSetInt_ExternalRead_Matches()
        {
            var test = new TestLoxEngine();

            test.Run("var val = 1;", true);

            var val = test.loxEngine.GetValue("val");

            Assert.AreEqual(1, val);
        }

        [Test]
        public void EngineSetString_ExternalRead_Matches()
        {
            var test = new TestLoxEngine();

            test.Run(@"var val = ""hello"";", true);

            var val = test.loxEngine.GetValue("val");

            Assert.AreEqual("hello", val);
        }

        [Test]
        public void ExternalCall_EngineFunc_Matches()
        {
            var test = new TestLoxEngine();

            test.Run(@"fun Foo(){print 1;}", true);

            var funcRes = test.loxEngine.CallFunction("Foo");

            Assert.AreEqual("1", test.InterpreterResult);
        }


        [Test]
        public void ExternalCall_WithParam_EngineFunc_Matches()
        {
            var test = new TestLoxEngine();

            test.Run(@"fun Foo(v){print v;}", true);

            var funcRes = test.loxEngine.CallFunction("Foo","hello");

            Assert.AreEqual("hello", test.InterpreterResult);
        }

        [Test]
        public void ExternalCall_WithParams_EngineFunc_Matches()
        {
            var test = new TestLoxEngine();

            test.Run(@"fun AddPrint(a,b){print a+b;}", true);

            var funcRes = test.loxEngine.CallFunction("AddPrint", 7, 3.14);

            Assert.AreEqual("10.14", test.InterpreterResult);
        }

        [Test]
        public void EngineCall_ExternalFunction_Matches()
        {
            var test = new TestLoxEngine();
            object res = false;

            test.loxEngine.SetValue("Func", new Callable(() => res = true));

            test.Run(@"Func();", true);

            Assert.AreEqual(res, true);
        }

        [Test]
        public void EngineCall_ExternalFunction_WithParam_Matches()
        {
            var test = new TestLoxEngine();
            object res = false;

            test.loxEngine.SetValue("Func", new Callable(1,(args) => { res = args[0]; }));

            test.Run(@"Func(true);", true);

            Assert.AreEqual(res, true);
        }

        [Test]
        public void EngineCall_ExternalFunction_WithParamAndReturn_Matches()
        {
            var test = new TestLoxEngine();

            test.loxEngine.SetValue("Func", new Callable(2, (args) => args[0].ToString() + args[1].ToString() ));

            test.Run(@"print Func(1,2);", true);

            Assert.AreEqual("12", test.InterpreterResult);
        }
    }
}