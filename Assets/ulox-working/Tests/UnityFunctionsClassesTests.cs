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
@"print (GetKey(""space""));",
@"False")
                .SetName("NoKey");

            yield return new TestCaseData(
@"print (CreateFromPrefab(""NO_SUCH_PREFAB"") == null);",
@"Unable to find prefab in 'UnityFunctionsLibrary' named 'NO_SUCH_PREFAB'.")
                .SetName("NoPrefab");

            yield return new TestCaseData(
@"SetGameObjectPosition(null, 1 ,2, 3);",
@"Unable to SetGameObjectPosition in 'UnityFunctionsLibrary'. Provided arg 0 is not a gameobject or component: 'null'.")
                .SetName("NoGOToSet");

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
                : base(new StandardClassesLibrary(),
                      new UnityFunctionsLibrary(new List<UnityEngine.GameObject>()))
            {
            }
        }
    }
}
