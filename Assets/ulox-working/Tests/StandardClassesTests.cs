using NUnit.Framework;
using System.Collections.Generic;

namespace ULox.Tests
{
    public class StandardClassesTests
    {
        public static IEnumerable<TestCaseData> Generator()
        {
            yield return new TestCaseData(
@"var arr = Array(3);
print (arr);",
@"<array [null,null,null,]>")
                .SetName("ArrayEmpty");

            yield return new TestCaseData(
@"var arr = Array(3);
print (arr.Get(arr.Count()-1));",
@"null")
                .SetName("ArrayEmpty_LastNull");

            yield return new TestCaseData(
@"var arr = Array(5);
for(var i = 0; i < arr.Count(); i += 1)
{
    arr.Set(i,i);
}
print (arr);",
@"<array [0,1,2,3,4,]>")
                .SetName("ArrayPrint");

            yield return new TestCaseData(
@"var arr = Array(5);

print (arr.Get(6));",
@"Index was out of range. Must be non-negative and less than the size of the collection.
Parameter name: index")
                .SetName("ArrayBoundsError");

            yield return new TestCaseData(
@"var list = List();
for(var i = 0; i < 10; i += 1) { list.Add(i); }

list.RemoveAt(4);
list.Remove(0);

for(var i = 0; i < list.Count(); i += 1) { print (list.Get(i)); }

print (list);
",
@"12356789<list [1,2,3,5,6,7,8,9,]>")
                .SetName("List_Fill_Validate");

            yield return new TestCaseData(
@"var pod = POD();
pod.a = 10;
print (pod.a);",
@"10")
                .SetName("POD_Store_Validate");

            yield return new TestCaseData(
@"var outter = POD();
outter.inner = POD();
outter.inner.a = 10;
print (outter.inner.a);",
@"10")
                .SetName("PODNested_Store_Validate");

            yield return new TestCaseData(
@"print ("""");",
@"")
                .SetName("Empty");
        }

        [Test]
        [TestCaseSource(nameof(Generator))]
        public void Engine_StringifiedResult_Matches(string testString, string requiredResult)
        {
            var engine = new StandardClassesTestLoxEngine();

            engine.Run(testString, true);

            Assert.AreEqual(requiredResult, engine.InterpreterResult);
        }

        [Test]
        public void ExternalFetch_PODNested_Store_Validate()
        {
            var test = new StandardClassesTestLoxEngine();

            test.Run(
@"var outter = POD();
outter.inner = POD();
outter.inner.a = 10;", true);

            Assert.AreEqual(10, test._engine.GetValue("outter.inner.a"));
        }

        [Test]
        public void ExternalSet_PODNested_Fetch_Validate()
        {
            var test = new StandardClassesTestLoxEngine();
            var testValue = "10";

            //prefered method for multiple creates
            var podClass = test._engine.GetClass("POD");
            test._engine.SetValue("outter", test._engine.CreateInstance(podClass));

            //convenient method for single shot
            test._engine.SetValue("outter.inner", test._engine.CreateInstance("POD"));

            test._engine.SetValue("outter.inner.a", testValue);

            test.Run(@"print (outter.inner.a);", true);

            Assert.AreEqual(testValue, test.InterpreterResult);
        }

        [Test]
        public void Global_ExternalPOD_InternalPOD_CollisionError()
        {
            var test = new StandardClassesTestLoxEngine();
            var testValue = "10";

            //prefered method for multiple creates
            var podClass = test._engine.GetClass("POD");
            test._engine.SetValue("collide", test._engine.CreateInstance(podClass));

            test._engine.SetValue("collide.a", testValue);

            test.Run(@"var collide = List();", true);

            Assert.IsTrue(test.InterpreterResult.StartsWith("IDENTIFIER|1:12 Environment value redefinition not allowed, \'collide\' collided."));
        }

        [Test]
        public void Inner_ExternalPOD_InternalPOD_Replace()
        {
            var test = new StandardClassesTestLoxEngine();
            var testValue = "10";

            //prefered method for multiple creates
            var podClass = test._engine.GetClass("POD");
            test._engine.SetValue("collide", test._engine.CreateInstance(podClass));
            test._engine.SetValue("collide.inner", test._engine.CreateInstance(podClass));

            test._engine.SetValue("collide.inner.a", testValue);

            test.Run(@"print (collide.inner); collide.inner = List(); print (collide.inner);", true);

            Assert.AreEqual("<inst POD><list []>", test.InterpreterResult);
        }

        internal class StandardClassesTestLoxEngine : TestLoxEngine
        {
            public StandardClassesTestLoxEngine()
                : base(new StandardClasses())
            {
            }
        }
    }
}
