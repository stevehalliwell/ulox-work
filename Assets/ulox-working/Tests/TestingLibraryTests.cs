using NUnit.Framework;
using System.Collections.Generic;

namespace ULox.Tests
{
    public class TestingLibraryTests
    {
        public static IEnumerable<TestCaseData> Generator()
        {
            yield return new TestCaseData(
@"print (GenerateTestingReport());",
@"No Testing Report Available.")
                .SetName("NoTestsResults");

            yield return new TestCaseData(
@"print ("""");",
@"")
                .SetName("Empty");
        }

        [Test]
        [TestCaseSource(nameof(Generator))]
        public void Engine_StringifiedResult_Matches(string testString, string requiredResult)
        {
            var engine = new TestingLibraryTestLoxEngine();

            engine.Run(testString, true);

            Assert.AreEqual(requiredResult, engine.InterpreterResult);
        }

        [Test]
        public void ExternalFileTests_Result_Failure()
        {
            var engine = new TestingLibraryTestLoxEngine();

            Assert.Throws<PanicException>(() =>
             engine.Run(System.IO.File.ReadAllText("Assets\\ulox-working\\Tests\\ULoxs\\Failing.ulox.txt"), true));
        }

        [Test]
        public void ExternalFileTests_Result_FailureWithMsg()
        {
            var engine = new TestingLibraryTestLoxEngine();

            Assert.Throws<PanicException>(() =>
             engine.Run(System.IO.File.ReadAllText("Assets\\ulox-working\\Tests\\ULoxs\\FailingWithMsg.ulox.txt"), true));
        }

        [Test]
        public void ExternalFileTests_Asserts_Failure()
        {
            var engine = new TestingLibraryTestLoxEngine();

            Assert.Throws<PanicException>(() =>
             engine.Run(System.IO.File.ReadAllText("Assets\\ulox-working\\Tests\\ULoxs\\FailingAsserts.ulox.txt"), true));
        }

        internal class TestingLibraryTestLoxEngine : TestLoxEngine
        {
            public TestingLibraryTestLoxEngine()
                : base(new TestingLibrary(true), new AssertLibrary())
            {
            }
        }
    }
}
