using NUnit.Framework;
using System.Collections.Generic;

namespace ULox.Tests
{
    public class UnityFunctionsClassesTests
    {
        public static IEnumerable<TestCaseData> Generator()
        {
            yield return new TestCaseData(
@"var v = Rand();
if(v < 1 and v >=0) { print (true); }",
@"True")
                .SetName("Rand_Valid");

            yield return new TestCaseData(
@"var v = RandRange(-1,3);
if(v <= 3 and v >= -1) { print (true); }",
@"True")
                .SetName("RandRange_Valid");

            yield return new TestCaseData(
@"print ("""");",
@"")
                .SetName("Empty");
        }

        [Test]
        [TestCaseSource(nameof(Generator))]
        public void Engine_StringifiedResult_Matches(string testString, string requiredResult)
        {
            var engine = new UnityFunctionsClassesTestLoxEngine();

            engine.Run(testString, true);

            Assert.AreEqual(requiredResult, engine.InterpreterResult);
        }

        internal class UnityFunctionsClassesTestLoxEngine : TestLoxEngine
        {
            public UnityFunctionsClassesTestLoxEngine()
                : base(new StandardClasses(),
                      new UnityFunctions())
            {
            }
        }
    }
}
