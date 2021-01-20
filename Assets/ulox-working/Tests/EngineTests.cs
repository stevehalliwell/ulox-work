using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ULox.Tests
{
    public class EngineTests
    {
        public static IEnumerable<TestCaseData> Generator()
        {
            yield return new TestCaseData(
@"abort();",
@"abort")
                .SetName("Abort");

            yield return new TestCaseData(
@"var start = clock();
sleep(50);
var end = clock();
if (end > start) { print (true); }",
@"True")
                .SetName("Clock_Sleep_GreaterTime");

            yield return new TestCaseData(
@"var a = 10;
printr (a);",
@"10")
                .SetName("PrintR_var");

            yield return new TestCaseData(
@"class Test{getset a;}
printr (Test);",
@"<class Test>
  <fn a>
  <fn Seta>")
                .SetName("PrintR_Class");

            yield return new TestCaseData(
@"class Test{getset a;}
printr (Test());",
@"<inst Test>
  _a : null")
                .SetName("PrintR_Inst");

            yield return new TestCaseData(
@"class Test{getset a;var b = 10;}
var t = Test();
t.Seta(Test());
printr (t);",
@"<inst Test>
  _a : <inst Test>
    _a : null
    b : 10
  b : 10")
                .SetName("PrintR_InstNested");

            yield return new TestCaseData(
@"class Base {getset a;}
class Test < Base{getset b;}
printr (Test);",
@"<class Test>
  meta : <class Base>
    <fn a>
    <fn Seta>
  <fn b>
  <fn Setb>")
                .SetName("PrintR_Class_WithBase");

            yield return new TestCaseData(
@"class Base {var c = 1;}
class Test < Base{getset a;var b = 10;}
var t = Test();
t.Seta(Test());
printr (t);",
@"<inst Test>
  c : 1
  _a : <inst Test>
    c : 1
    _a : null
    b : 10
  b : 10")
                .SetName("PrintR_InstNested_WithBase");

            yield return new TestCaseData(
@"class Test {var a;}
var insta = Test();
var instb = Test();

insta.a = instb;
instb.a = insta;

printr (insta);",
@"<inst Test>
  a : <inst Test>
    a : <inst Test>
      a : <inst Test>
        a : <inst Test>
          a : <inst Test>
            a : <inst Test>
              a : <inst Test>
                a : <inst Test>
                  a : <inst Test>
                    a : MAX_DEPTH_REACHED
")
                .SetName("PrintR_CircluarRef");

            yield return new TestCaseData(
@"print ("""");",
@"")
                .SetName("Empty");
        }

        [Test]
        public void CallableAction_Validate()
        {
            var test = new EngineTestLoxEngine();
            var res = 0;

            test._engine.SetValue("TestAction", new Callable(() => { res = 1; }));

            test.Run("TestAction();", true);

            Assert.AreEqual(1, res);
        }

        [Test]
        public void CallableFullFunc_Validate()
        {
            var test = new EngineTestLoxEngine();
            object[] arguments = null;

            test._engine.SetValue("TestAction", new Callable(1, (args) =>
            {
                arguments = args;
                return (string)args[0] + "World!";
            }));

            test.Run(@"print( TestAction(""Hello ""));", true);

            Assert.AreEqual("Hello World!", test.InterpreterResult);
        }

        [Test]
        [TestCaseSource(nameof(Generator))]
        public void Engine_StringifiedResult_Matches(string testString, string requiredResult)
        {
            var engine = new EngineTestLoxEngine();

            engine.Run(testString, true);

            Assert.AreEqual(requiredResult, engine.InterpreterResult);
        }

        [Test]
        public void EngineCall_ExternalFunction_Matches()
        {
            var test = new EngineTestLoxEngine();
            object res = false;

            test._engine.SetValue("Func", new Callable(() => res = true));

            test.Run(@"Func();", true);

            Assert.AreEqual(res, true);
        }

        [Test]
        public void EngineCall_ExternalFunction_WithParam_Matches()
        {
            var test = new EngineTestLoxEngine();
            object res = false;

            test._engine.SetValue("Func", new Callable(1, (args) => { res = args[0]; }));

            test.Run(@"Func(true);", true);

            Assert.AreEqual(res, true);
        }

        [Test]
        public void EngineCall_ExternalFunction_WithParamAndReturn_Matches()
        {
            var test = new EngineTestLoxEngine();

            test._engine.SetValue("Func", new Callable(2, (args) => args[0].ToString() + args[1].ToString()));

            test.Run(@"print (Func(1,2));", true);

            Assert.AreEqual("12", test.InterpreterResult);
        }

        [Test]
        public void EngineSetInt_ExternalRead_Matches()
        {
            var test = new EngineTestLoxEngine();

            test.Run("var val = 1;", true);

            var val = test._engine.GetValue("val");

            Assert.AreEqual(1, val);
        }

        [Test]
        public void EngineSetString_ExternalRead_Matches()
        {
            var test = new EngineTestLoxEngine();

            test.Run(@"var val = ""hello"";", true);

            var val = test._engine.GetValue("val");

            Assert.AreEqual("hello", val);
        }

        [Test]
        public void ExternalBool_EngineRead_Matches()
        {
            var test = new EngineTestLoxEngine();
            bool extVar = true;

            test._engine.SetValue(nameof(extVar), extVar);

            test.Run(@"print (extVar or """");", true);

            Assert.AreEqual(extVar.ToString(), test.InterpreterResult);
        }

        [Test]
        public void ExternalCall_EngineFunc_Matches()
        {
            var test = new EngineTestLoxEngine();

            test.Run(@"fun Foo(){print (1);}", true);

            var funcRes = test._engine.CallFunction("Foo");

            Assert.AreEqual("1", test.InterpreterResult);
        }

        [Test]
        public void ExternalCall_WithParam_EngineFunc_Matches()
        {
            var test = new EngineTestLoxEngine();

            test.Run(@"fun Foo(v){print (v);}", true);

            var funcRes = test._engine.CallFunction("Foo", "hello");

            Assert.AreEqual("hello", test.InterpreterResult);
        }

        [Test]
        public void ExternalCall_WithParams_EngineFunc_Matches()
        {
            var test = new EngineTestLoxEngine();

            test.Run(@"fun AddPrint(a,b){print (a+b);}", true);

            var funcRes = test._engine.CallFunction("AddPrint", 7, 3.14);

            Assert.AreEqual("10.14", test.InterpreterResult);
        }

        [Test]
        public void ExternalCreate_ScriptDeclaredClass_Validate()
        {
            var test = new EngineTestLoxEngine();
            test.Run(
@"class TestClass{var val = 1;}", true, false);

            //create
            test._engine.SetValue("testInstance", test._engine.CreateInstance("TestClass"));

            //use
            test.Run(
@"print (testInstance.val);", true, false);

            Assert.AreEqual("1", test.InterpreterResult);
        }

        [Test]
        public void ExternalFloat_EngineRead_Matches()
        {
            var test = new EngineTestLoxEngine();
            float extVar = 3.14f;

            test._engine.SetValue(nameof(extVar), extVar);

            test.Run(@"print (extVar or """");", true);

            Assert.AreEqual(extVar.ToString().Substring(0, 4), test.InterpreterResult.Substring(0, 4));
        }

        [Test]
        public void ExternalInt_EngineRead_Matches()
        {
            var test = new EngineTestLoxEngine();
            int extVar = 7;

            test._engine.SetValue(nameof(extVar), extVar);

            test.Run(@"print (extVar or """");", true);

            Assert.AreEqual(extVar.ToString(), test.InterpreterResult);
        }

        [Test]
        public void ExternalString_EngineRead_Matches()
        {
            var test = new EngineTestLoxEngine();
            string extVar = "test";

            test._engine.SetValue(nameof(extVar), extVar);

            test.Run(@"print (extVar or """");", true);

            Assert.AreEqual(extVar.ToString(), test.InterpreterResult);
        }

        [Test]
        public void ExternalUpdateValue_EngineRead_Matches()
        {
            var test = new EngineTestLoxEngine();
            var initialTestString =
@"var v = true;
print(v);";

            test.Run(initialTestString, true);

            test._engine.SetValue("v", false);

            test.Run("print (v);", true);

            Assert.AreEqual("TrueFalse", test.InterpreterResult);
        }

        internal class EngineTestLoxEngine : TestLoxEngine
        {
            public EngineTestLoxEngine()
                : base()
            {
            }
        }
    }
}
