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

            Assert.IsTrue(test.InterpreterResult.StartsWith("Environment value redefinition not allowed. Requested Test:1 collided."));
        }

        //todo test with push local add local func refs, run script in local sandbox, takes the local env, disconnects from enclosing, runs, reconnects enclosing
        [Test]
        public void RunInSandbox_GlobalUnchanged_Validate()
        {
            var test = new CustomEnvironmentTestLoxEngine();
            test.Run(@"
var myvar = 10;
PushLocalEnvironment();
var print = print;
var innerScript = ""var myvar = 5; print(myvar); print = null;"";
RunScriptInLocalSandbox(innerScript);
PopLocalEnvironment();
print(myvar);", true);
            
            Assert.AreEqual("510", test.InterpreterResult);
        }

        internal class CustomEnvironmentTestLoxEngine : TestLoxEngine
        {
            public CustomEnvironmentTestLoxEngine()
                : base(new EngineFunctions())
            {
            }
        }
    }
}
