using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ULox.Tests
{
    public class ByteCodeLoxEngineTests
    {
        public static Chunk GenerateManualChunk()
        {
            var chunk = new Chunk("main");

            chunk.WriteConstant(Value.New(0.5), 1);
            chunk.WriteConstant(Value.New(1), 1);
            chunk.WriteSimple(OpCode.NEGATE,1);
            chunk.WriteSimple(OpCode.ADD,1);
            chunk.WriteConstant(Value.New(2), 1);
            chunk.WriteSimple(OpCode.MULTIPLY, 1);
            chunk.WriteSimple(OpCode.RETURN, 2);

            return chunk;
        }

        [Test]
        public void Manual_Chunk_Disasemble()
        {
            var chunk = GenerateManualChunk();
            var dis = new Disasembler();

            dis.DoChunk(ref chunk);

            Debug.Log(dis.GetString());
        }

        [Test]
        public void Manual_Chunk_VM()
        {
            var chunk = GenerateManualChunk();
            VM vm = new VM();

            Assert.AreEqual(vm.Interpret(chunk), InterpreterResult.OK);
        }

        [Test]
        public void Engine_Cycle_Math_Expression()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run("1+2");

            Assert.AreEqual(engine.StackDump, "3");
        }

        [Test]
        public void Engine_Cycle_Logic_Not_Expression()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run("!true");

            Assert.AreEqual(engine.StackDump, "False");
        }

        [Test]
        public void Engine_Cycle_Logic_Compare_Expression()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run("1 < 2 == false");

            Assert.AreEqual(engine.StackDump, "False");
        }

        [Test]
        public void Engine_Cycle_String_Add_Expression()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run("\"hello\" + \" \" + \"world\"");

            Assert.AreEqual(engine.StackDump, "hello world");
        }

        [Test]
        public void Engine_Cycle_Print_Math_Expression()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run("print 1 + 2 * 3;");

            //Assert.AreEqual(engine.StackDump, "False");
        }

        public class ByteCodeLoxEngine
        {
            private Scanner _scanner;
            private Compiler _compiler;
            private VM _vm;

            public ByteCodeLoxEngine()
            {
                _scanner = new Scanner();
                _compiler = new Compiler();
                _vm = new VM();
            }

            public string InterpreterResult { get; private set; } = string.Empty;
            public string StackDump => _vm.GenerateStackDump();

            protected void AppendResult(string str) => InterpreterResult += str;
            public virtual void Run(string testString)
            {
                try
                {
                   var tokens = _scanner.Scan(testString);
                    var chunk = new Chunk("main");
                    _compiler.Compile(chunk, tokens);
                    _vm.Interpret(chunk);
                }
                catch (LoxException e)
                {
                    AppendResult(e.Message);
                }
                catch (System.ArgumentOutOfRangeException e)
                {
                    AppendResult(e.Message);
                }
                catch (System.IndexOutOfRangeException e)
                {
                    AppendResult(e.Message);
                }
            }
        }
    }
}