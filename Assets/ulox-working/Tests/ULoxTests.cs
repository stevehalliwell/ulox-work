using NUnit.Framework;
using System.Collections.Generic;

namespace ULox.Tests
{
    public class ULoxTests
    {
        public static string[] FileList = new string[]
        {
            "Assets\\ulox-working\\Tests\\ULoxs\\IntegerMathTests.ulox.txt",
            "Assets\\ulox-working\\Tests\\ULoxs\\DoubleMathTests.ulox.txt",
        };

        [Test]
        public void Run_ULox_File_Tests()
        {
            var engine = new ULoxTestLoxEngine();

            foreach (var file in FileList)
            {
                engine.Run(System.IO.File.ReadAllText(file), true);
            }
        }

        internal class ULoxTestLoxEngine : TestLoxEngine
        {
            public ULoxTestLoxEngine()
                : base(new TestingLibrary(true), 
                      new AssertLibrary(), 
                      new EngineFunctions(), 
                      new StandardClasses()) { }
        }
    }
}
