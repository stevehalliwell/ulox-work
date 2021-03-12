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

            dis.DoChunk(chunk);

            Debug.Log(dis.GetString());
        }

        [Test]
        public void Manual_Chunk_VM()
        {
            var chunk = GenerateManualChunk();
            VM vm = new VM(null);

            Assert.AreEqual(vm.Interpret(chunk), InterpreterResult.OK);
        }

        [Test]
        public void Engine_Cycle_Math_Expression()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run("print 1+2;");

            Assert.AreEqual(engine.InterpreterResult, "3");
        }

        [Test]
        public void Engine_Cycle_Logic_Not_Expression()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run("print !true;");

            Assert.AreEqual(engine.InterpreterResult, "False");
        }

        [Test]
        public void Engine_Cycle_Logic_Compare_Expression()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run("print 1 < 2 == false;");

            Assert.AreEqual(engine.InterpreterResult, "False");
        }

        [Test]
        public void Engine_Cycle_String_Add_Expression()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run("print \"hello\" + \" \" + \"world\";");

            Assert.AreEqual(engine.InterpreterResult, "hello world");
        }

        [Test]
        public void Engine_Cycle_Print_Math_Expression()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run("print 1 + 2 * 3;");

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

        [Test]
        public void Engine_Cycle_Global_Var()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"var myVar = 10; 
var myNull; 
print myVar; 
print myNull;

var myOtherVar = myVar * 2;

print myOtherVar;");

            Assert.AreEqual(engine.InterpreterResult, "10null20");
        }

        [Test]
        public void Engine_Cycle_Blocks_Constants()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"{print 1+2;}");

            Assert.AreEqual(engine.InterpreterResult, "3");
        }

        [Test]
        public void Engine_Cycle_Blocks_Globals()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
var a = 2; 
var b = 1;
{
    print a+b;
}");

            Assert.AreEqual(engine.InterpreterResult, "3");
        }

        [Test]
        public void Engine_Cycle_Blocks_Locals()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
{
    var a = 2; 
    var b = 1;
    print a+b;
    {
        var c = 3;
        print a+b+c;
    }
}");

            Assert.AreEqual(engine.InterpreterResult, "36");
        }

        [Test]
        public void Engine_Cycle_If_Jump_Constants()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"if(1 > 2) print ""ERROR""; print ""End"";");

            Assert.AreEqual(engine.InterpreterResult, "End");
        }

        [Test]
        public void Engine_Cycle_If_Else_Constants()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
if(1 > 2) 
    print ""ERROR""; 
else 
    print ""The ""; 
print ""End"";");

            Assert.AreEqual(engine.InterpreterResult, "The End");
        }

        [Test]
        public void Engine_Cycle_If_Else_Logic_Constants()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
if(1 > 2 or 2 > 3) 
    print ""ERROR""; 
else if (1 == 1 and 2 == 2)
    print ""The ""; 
print ""End"";");

            Assert.AreEqual(engine.InterpreterResult, "The End");
        }

        [Test]
        public void Engine_Cycle_While()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
var i = 0;
while(i < 2)
{
    print ""hip, "";
    i = i + 1;
}

print ""hurray"";");


            Assert.AreEqual(engine.InterpreterResult, "hip, hip, hurray");
        }

        public class ByteCodeLoxEngine
        {
            private Scanner _scanner;
            private Compiler _compiler;
            private VM _vm;
            private Disasembler _disasembler;

            public ByteCodeLoxEngine()
            {
                _scanner = new Scanner();
                _compiler = new Compiler();
                _disasembler = new Disasembler();
                _vm = new VM(AppendResult);
            }

            public string InterpreterResult { get; private set; } = string.Empty;
            public string StackDump => _vm.GenerateStackDump();
            public string Disassembly => _disasembler.GetString();

            protected void AppendResult(string str) => InterpreterResult += str;
            public virtual void Run(string testString)
            {
                try
                {
                    var tokens = _scanner.Scan(testString);
                    var chunk = new Chunk("main");
                    _compiler.Compile(chunk, tokens);
                    _disasembler.DoChunk(chunk);
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
                catch (System.Exception e)
                {
                    throw e;
                }
            }
        }
    }
}