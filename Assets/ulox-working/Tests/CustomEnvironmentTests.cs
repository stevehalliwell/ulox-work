using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ULox.Tests
{
    public class CustomEnvironmentTests
    {
        [Test]
        public void LocalEnvironment_ReadGlobal_Success()
        {
            var test = new CustomEnvironmentTestLoxEngine();

            test.Run("var a = 5;", true);

            var localEnv = new ULoxScriptEnvironment(test._engine);
            localEnv.RunScript("print(a);");

            Assert.AreEqual("5", test.InterpreterResult);
        }

        [Test]
        public void LocalEnvironment_Shadowing_Success()
        {
            var test = new CustomEnvironmentTestLoxEngine();

            test.Run("var a = 5;", true);

            var localEnv = new ULoxScriptEnvironment(test._engine);
            localEnv.RunScript("var a = 10;");
            localEnv.RunScript("print(a);");

            test.Run("print(a);", true);

            Assert.AreEqual("105", test.InterpreterResult);
        }

        [Test]
        public void LocalEnvironment_CallWithShadowedUpValue_FindsGlobal_Success()
        {
            //Functions are closures so they've already grabbed global as their closure here and don't care about the local
            var test = new CustomEnvironmentTestLoxEngine();

            test.Run("var a = 5; fun Test(){print(a);}", true);

            var localEnv = new ULoxScriptEnvironment(test._engine);
            localEnv.RunScript("var a = 10;");

            var testFunc = test._engine.GetValue("Test");
            localEnv.CallFunction(testFunc as ICallable);

            test._engine.CallFunction(testFunc as ICallable);

            Assert.AreEqual("55", test.InterpreterResult);
        }

        [Test]
        public void LocalEnvironment_CallWithShadowedUpValue_FindsGlobal_ReversedExecutionOrder_Success()
        {
            //Functions are closures so they've already grabbed global as their closure here and don't care about the local
            var test = new CustomEnvironmentTestLoxEngine();

            test.Run("var a = 5; fun Test(){print(a);}", true);

            var localEnv = new ULoxScriptEnvironment(test._engine);
            localEnv.RunScript("var a = 10;");

            var testFunc = test._engine.GetValue("Test");
            test._engine.CallFunction(testFunc as ICallable);
            localEnv.CallFunction(testFunc as ICallable);

            Assert.AreEqual("55", test.InterpreterResult);
        }

        [Test]
        public void LocalEnvironment_GlobalFunc_FindsLocal_Fails()
        {
            //again as func is a closure around global it cannot find the one inside it's environment
            var test = new CustomEnvironmentTestLoxEngine();

            test.Run("fun Test(){print(a);}", true);

            var localEnv = new ULoxScriptEnvironment(test._engine);
            localEnv.RunScript("var a = 10;");

            var testFunc = test._engine.GetValue("Test");

            try
            {
                localEnv.CallFunction(testFunc as ICallable);
                test._engine.CallFunction(testFunc as ICallable);
            }
            catch (EnvironmentException e)
            {
                Assert.IsTrue(e.Message.Contains("Undefined variable a"));
            }
            catch (System.Exception)
            { 
                throw; 
            }
        }

        [Test]
        public void LocalEnvironment_To_LocalEnv_Via_Globals()
        {
            var test = new CustomEnvironmentTestLoxEngine();

            var localEnvA = new ULoxScriptEnvironment(test._engine);
            var localEnvB = new ULoxScriptEnvironment(test._engine);

            localEnvA.RunScript("Globals.val = 10;");
            localEnvB.RunScript("print(Globals.val);");

            Assert.AreEqual("10", test.InterpreterResult);
        }

        [Test]
        public void LocalEnvironment_ExternalInteractions()
        {
            var test = new CustomEnvironmentTestLoxEngine();

            var localEnv = new ULoxScriptEnvironment(test._engine);

            localEnv.RunScript("var val = 10;");

            var val = localEnv.FetchLocalByName("val");
            localEnv.AssignLocalByName("val", 20);
            localEnv.AssignLocalByName("val2", 30);
            localEnv.RunScript("val2 *= 2;");

            Assert.AreEqual(val, 10);
            Assert.AreEqual(localEnv.FetchLocalByName("val"), 20);
            Assert.AreEqual(localEnv.FetchLocalByName("val2"), 60);
        }

        [Test]
        public void RunScript_InsideScript_Validate()
        {
            var test = new CustomEnvironmentTestLoxEngine();
            var testString = @"
var a = 5;
fun Test(b){print(a+b);}

RunScript(""Test(5);"");";

            test.Run(testString, true);

            Assert.AreEqual("10", test.InterpreterResult);
        }

        [Test]
        public void RunSameScript_SameEngine_SeparateEnvironments()
        {
            var test = new CustomEnvironmentTestLoxEngine();
            var initialTestString = @"fun Test(){print(1);}";
            var envs = new List<IEnvironment>();

            for (int i = 0; i < 3; i++)
            {
                envs.Add(test._engine.Interpreter.PushNewEnvironemnt());
                test.Run(initialTestString, true);
                test._engine.Interpreter.PopSpecificEnvironemnt(envs.Last());
            }

            var secondTestString = @"Test();";

            for (int i = 0; i < 3; i++)
            {
                test._engine.Interpreter.PushEnvironemnt(envs[i]);
                test.Run(secondTestString, true);
                test._engine.Interpreter.PopEnvironemnt();
            }

            Assert.AreEqual("111", test.InterpreterResult);
        }


        [Test]
        public void RunScript_InSandbox_FailsToFindGlobal()
        {
            var test = new CustomEnvironmentTestLoxEngine();

            test.Run("print(5);", true);

            test._engine.Interpreter.PushEnvironemnt(new Environment(null));
            test.Run("print(5);", true);
            test._engine.Interpreter.PopEnvironemnt();

            test.Run("print(5);", true);

            Assert.AreEqual("5IDENTIFIER|1:5 Undefined variable print5", test.InterpreterResult);
        }

        [Test]
        public void RunSameScript_SameEngine_SeparateEnvironments_WithCollisions()
        {
            var test = new CustomEnvironmentTestLoxEngine();
            var initialTestString = @"fun Test(){print(1);}";
            var envs = new List<IEnvironment>();

            for (int i = 0; i < 3; i++)
            {
                envs.Add(test._engine.Interpreter.PushNewEnvironemnt());
                test.Run(initialTestString, true);
                test._engine.Interpreter.PopSpecificEnvironemnt(envs.Last());
            }

            for (int i = 0; i < 3; i++)
            {
                test._engine.Interpreter.PushEnvironemnt(envs[i]);
                test.Run(initialTestString, true);
                test._engine.Interpreter.PopEnvironemnt();
            }

            Assert.IsTrue(test.InterpreterResult.StartsWith("IDENTIFIER|1:9 Environment value redefinition not allowed,"));
        }

        [Test]
        public void RunInSandbox_GlobalUnchanged_Validate()
        {
            var test = new CustomEnvironmentTestLoxEngine();
            test.Run(@"
var myvar = 10;
PushLocalEnvironment();
//put a print callable in the local scope
var print = print;
var innerScript = ""var myvar = 5; print(myvar); print = null;"";
RunScriptInLocalSandbox(innerScript);
PopLocalEnvironment();
print(myvar);", true);
            
            Assert.AreEqual("510", test.InterpreterResult);
        }

        [Test]
        public void RunInSandbox_OutterTakesData_Validate()
        {
            var test = new CustomEnvironmentTestLoxEngine();
            test.Run(@"
var innerScript = ""holder.data = 5;"";
var pod = POD();
PushLocalEnvironment();
var holder = pod;
RunScriptInLocalSandbox(innerScript);
PopLocalEnvironment();
print(pod.data);", true);

            Assert.AreEqual("5", test.InterpreterResult);
        }

        [Test]
        public void RunInLocal_AssigningToGlobal_Validate()
        {
            var test = new CustomEnvironmentTestLoxEngine();
            test.Run(@"
var innerScript = ""Globals.testVar = 5;"";
PushLocalEnvironment();
RunScript(innerScript);
PopLocalEnvironment();
print(Globals.testVar);
print(testVar);", true);

            Assert.AreEqual("55", test.InterpreterResult);
        }

        [Test]
        public void RunInSandbox_AssigningToGlobal_Fails()
        {
            var test = new CustomEnvironmentTestLoxEngine();
            test.Run(@"
var innerScript = ""Globals.testVar = 5;"";
PushLocalEnvironment();
RunScriptInLocalSandbox(innerScript);
PopLocalEnvironment();
print(Globals.testVar);
print(testVar);", true);

            Assert.IsTrue(test.InterpreterResult.Contains("Undefined variable Globals"));
        }

        internal class CustomEnvironmentTestLoxEngine : TestLoxEngine
        {
            public CustomEnvironmentTestLoxEngine()
                : base(new StandardClasses(), new EngineFunctions())
            {
            }
        }
    }
}
