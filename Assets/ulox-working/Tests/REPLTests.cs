using NUnit.Framework;
using System;
using System.Linq;

namespace ULox.Tests
{
    public class REPLTests
    {
        [Test]
        [TestCase(new object[]
        {
@"20/2;",
@"10" }, TestName = "Show_ExprResult")]
        [TestCase(new object[]
        {
@"var a = 10;",
@"a;",
@"10" }, TestName = "Var")]
        [TestCase(new object[]
        {
@"var a = 10;",
@"var b = 10;",
@"a + b;",
@"20" }, TestName = "Var_Math_Print")]
        [TestCase(new object[]
        {
@"fun T(){return 10;}",
@"T();",
@"10" }, TestName = "Func_Return")]
        [TestCase(new object[]
        {
@"fun T(a,b){return a+b;}",
@"T(5,5);",
@"10" }, TestName = "Func_Params_Return")]
        [TestCase(new object[]
        {
@"class TestClass{}",
@"TestClass;",
@"<class TestClass>" }, TestName = "Class")]
        [TestCase(new object[]
        {
@"class TestClass{}",
@"TestClass();",
@"<inst TestClass>" }, TestName = "Instance")]
        [TestCase(new object[]
        {
@"class TestClass{init(){this.a = 10;}}",
@"TestClass().a;",
@"10" }, TestName = "Instance_init_value")]
        [TestCase(new object[]
        {
@"class TestClass{var a = 10;}",
@"TestClass().a;",
@"10" }, TestName = "Instance_props_var_value")]
        [TestCase(new object[]
        {
@"class TestClass{getset a = 10;}",
@"var t = TestClass();",
@"t.Seta(t.a + 10);",   //null return
@"t.a;",
@"null20" }, TestName = "Instance_props_getset_value")]
        [TestCase(new object[]
        {
@"class TestClass{class var a = 10;}",
@"TestClass.a;",
@"10" }, TestName = "Class_metaField_var")]
        [TestCase(new object[]
        {
@"",
@"",
@"" }, TestName = "Empty")]
        public void Engine_StringifiedResult_Matches(params string[] testStrings)
        {
            var engine = new REPLTestLoxEngine();

            for (int i = 0; i < testStrings.Length - 1; i++)
            {
                engine.Run(testStrings[i]);
            }

            Assert.AreEqual(testStrings.Last(), engine.InterpreterResult);
        }

        internal class REPLTestLoxEngine : TestLoxEngine
        {
            public override void Run(string testString, bool catchAndLogExceptions = true, bool logWarnings = true, Action<string> REPLPrint = null)
            {
                base.Run(testString, catchAndLogExceptions, logWarnings, SetResult);
            }
        }
    }
}
