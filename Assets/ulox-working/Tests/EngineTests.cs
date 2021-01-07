using NUnit.Framework;
using System.Collections.Generic;

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

            var funcRes = test.loxEngine.CallFunction("Foo", "hello");

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

            test.loxEngine.SetValue("Func", new Callable(1, (args) => { res = args[0]; }));

            test.Run(@"Func(true);", true);

            Assert.AreEqual(res, true);
        }

        [Test]
        public void EngineCall_ExternalFunction_WithParamAndReturn_Matches()
        {
            var test = new TestLoxEngine();

            test.loxEngine.SetValue("Func", new Callable(2, (args) => args[0].ToString() + args[1].ToString()));

            test.Run(@"print Func(1,2);", true);

            Assert.AreEqual("12", test.InterpreterResult);
        }

        [Test]
        public void ExternalFetch_PODNested_Store_Validate()
        {
            var test = new TestLoxEngine();

            test.Run(
@"var outter = POD();
outter.inner = POD();
outter.inner.a = 10;", true);

            Assert.AreEqual(10, test.loxEngine.GetValue("outter.inner.a"));
        }

        [Test]
        public void Global_ExternalPOD_InternalPOD_CollisionError()
        {
            var test = new TestLoxEngine();
            var testValue = "10";

            //prefered method for multiple creates
            var podClass = test.loxEngine.GetClass("POD");
            test.loxEngine.SetValue("collide", test.loxEngine.CreateInstance(podClass));

            test.loxEngine.SetValue("collide.a", testValue);

            test.Run(@"var collide = List();", true);

            Assert.IsTrue(test.InterpreterResult.StartsWith("Environment value redinition not allowed."));
        }

        [Test]
        public void Inner_ExternalPOD_InternalPOD_Replace()
        {
            var test = new TestLoxEngine();
            var testValue = "10";

            //prefered method for multiple creates
            var podClass = test.loxEngine.GetClass("POD");
            test.loxEngine.SetValue("collide", test.loxEngine.CreateInstance(podClass));
            test.loxEngine.SetValue("collide.inner", test.loxEngine.CreateInstance(podClass));

            test.loxEngine.SetValue("collide.inner.a", testValue);

            test.Run(@"print collide.inner; collide.inner = List(); print collide.inner;", true);

            Assert.AreEqual("<inst POD><list []>", test.InterpreterResult);
        }

        [Test]
        public void ExternalSet_PODNested_Fetch_Validate()
        {
            var test = new TestLoxEngine();
            var testValue = "10";

            //prefered method for multiple creates
            var podClass = test.loxEngine.GetClass("POD");
            test.loxEngine.SetValue("outter", test.loxEngine.CreateInstance(podClass));

            //convenient method for single shot
            test.loxEngine.SetValue("outter.inner", test.loxEngine.CreateInstance("POD"));

            test.loxEngine.SetValue("outter.inner.a", testValue);

            test.Run(@"print outter.inner.a;", true);

            Assert.AreEqual(testValue, test.InterpreterResult);
        }

        [Test]
        public void ExternalCreate_ScriptDeclaredClass_Validate()
        {
            var test = new TestLoxEngine(); 
            test.Run(
 @"class TestClass{var val = 1;}", true, false);

            //create
            test.loxEngine.SetValue("testInstance", test.loxEngine.CreateInstance("TestClass"));

            //use
            test.Run(
 @"print testInstance.val;", true, false);

            Assert.AreEqual("1", test.InterpreterResult);
        }

        [Test]
        public void CallableAction_Validate()
        {
            var test = new TestLoxEngine();
            var res = 0;

            test.loxEngine.SetValue("TestAction", new Callable(()=> { res = 1; }));

            test.Run("TestAction();",true);

            Assert.AreEqual(1,res);
        }

        [Test]
        public void CallableFullFunc_Validate()
        {
            var test = new TestLoxEngine();
            Interpreter interpreter = null;
            object[] arguments = null;

            test.loxEngine.SetValue("TestAction", new Callable(1,(interp, args) => 
            {
                interpreter = interp;
                arguments = args;
                return (string)args[0] + "World!";
            }));

            test.Run(@"print TestAction(""Hello "");", true);

            Assert.AreEqual(interpreter, test.Interpreter);
            Assert.AreEqual("Hello World!", test.InterpreterResult);
        }

        public static IEnumerable<TestCaseData> Generator()
        {
            yield return new TestCaseData(
@"abort();",
@"abort")
                .SetName("Abort");

            yield return new TestCaseData(
@"var arr = Array(3);
print arr;",
@"<array [null,null,null,]>")
                .SetName("ArrayEmpty");

            yield return new TestCaseData(
@"var arr = Array(3);
print arr.Get(arr.Count()-1);",
@"null")
                .SetName("ArrayEmpty_LastNull");

            yield return new TestCaseData(
@"var arr = Array(5);
for(var i = 0; i < arr.Count(); i += 1)
{
    arr.Set(i,i);
}
print arr;",
@"<array [0,1,2,3,4,]>")
                .SetName("ArrayPrint");

            yield return new TestCaseData(
@"var arr = Array(5);

print arr.Get(6);",
@"Index was out of range. Must be non-negative and less than the size of the collection.
Parameter name: index")
                .SetName("ArrayBoundsError");

            yield return new TestCaseData(
@"var arr = Array(5);
arr.a = 2;",
@"NONE|-1:-1 Can't add properties to arrays.")
                .SetName("Array_CannotSet");

            yield return new TestCaseData(
@"var start = clock();
sleep(50);
var end = clock();
if (end > start) { print true; }",
@"True")
                .SetName("Clock_Sleep_GreaterTime");

            yield return new TestCaseData(
@"var v = Rand();
if(v < 1 and v >=0) { print true; }",
@"True")
                .SetName("Rand_Valid");

            yield return new TestCaseData(
@"var v = RandRange(-1,3);
if(v <= 3 and v >= -1) { print true; }",
@"True")
                .SetName("RandRange_Valid");

            yield return new TestCaseData(
@"var list = List();
for(var i = 0; i < 10; i += 1) { list.Add(i); }

list.RemoveAt(4);
list.Remove(0);

for(var i = 0; i < list.Count(); i += 1) { print list.Get(i); }

print list;
",
@"12356789<list [1,2,3,5,6,7,8,9,]>")
                .SetName("List_Fill_Validate");

            yield return new TestCaseData(
@"var pod = POD();
pod.a = 10;
print pod.a;",
@"10")
                .SetName("POD_Store_Validate");

            yield return new TestCaseData(
@"var outter = POD();
outter.inner = POD();
outter.inner.a = 10;
print outter.inner.a;",
@"10")
                .SetName("PODNested_Store_Validate");

            yield return new TestCaseData(
@"print """";",
@"")
                .SetName("Empty");
        }

        [Test]
        [TestCaseSource(nameof(Generator))]
        public void Engine_StringifiedResult_Matches(string testString, string requiredResult)
        {
            var engine = new TestLoxEngine();

            engine.Run(testString, true);

            Assert.AreEqual(requiredResult, engine.InterpreterResult);
        }
    }
}
