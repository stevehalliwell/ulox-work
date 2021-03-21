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
            chunk.WriteSimple(OpCode.NEGATE, 1);
            chunk.WriteSimple(OpCode.ADD, 1);
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

        [Test]
        public void Engine_Cycle_For()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
for(var i = 0; i < 2; i = i + 1)
{
    print ""hip, "";
}

print ""hurray"";");


            Assert.AreEqual(engine.InterpreterResult, "hip, hip, hurray");
        }

        [Test]
        public void Engine_Compile_Func_Do_Nothing()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
fun T()
{
    var a = 2;
}

print T;");

            Assert.AreEqual(engine.InterpreterResult, "<closure T>");
        }

        [Test]
        public void Engine_Compile_Func_Call()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
fun MyFunc()
{
    print 2;
}

MyFunc();");

            Assert.AreEqual(engine.InterpreterResult, "2");
        }

        [Test]
        public void Engine_Compile_NativeFunc_Call()
        {
            var engine = new ByteCodeLoxEngine();
            engine.VM.DefineNativeFunction("CallEmptyNative", (vm, stack) => Value.New("Native"));

            engine.Run(@"print CallEmptyNative();");

            Assert.AreEqual(engine.InterpreterResult, "Native");
        }

        [Test]
        public void Engine_Compile_Call_Mixed_Ops()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
fun A(){return 2;}
fun B(){return 3;}
fun C(){return 10;}

print A()+B()*C();");

            Assert.AreEqual(engine.InterpreterResult, "32");
        }

        [Test]
        public void Engine_Compile_Func_Inner_Logic()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
fun A(v)
{
    if(v > 5)
        return 2;
    return -1;
}
fun B(){return 3;}
fun C(){return 10;}

print A(1)+B()*C();

print A(10)+B()*C();");

            Assert.AreEqual(engine.InterpreterResult, "2932");
        }

        [Test]
        public void Engine_Compile_Var_Mixed_Ops()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
var a = 2;
var b = 3;
var c = 10;

print a+b*c;");

            Assert.AreEqual(engine.InterpreterResult, "32");
        }

        //[Test]
        //public void Engine_Compile_NativeFunc_Args_Call()
        //{
        //    var engine = new ByteCodeLoxEngine();
        //    engine.VM.DefineNativeFunction("CallNative", (vm, stack) =>
        //    {
        //        var lhs = vm.
        //        return Value.New("Native");
        //    });

        //    engine.Run(@"print CalEmptylNative();");

        //    Assert.AreEqual(engine.InterpreterResult, "Native");
        //}

        [Test]
        [Ignore("long running manual test only")]
        public void Engine_Compile_Clocked_Fib()
        {
            var engine = new ByteCodeLoxEngine();
            engine.VM.DefineNativeFunction("clock", (vm, stack) =>
            {
                return Value.New(System.DateTime.Now.Ticks);
            });

            engine.Run(@"
fun fib(n)
{
    if (n < 2) return n;
    return fib(n - 2) + fib(n - 1);
}

var start = clock();
print fib(20);
print "" in "";
print clock() - start;");

            //Assert.AreEqual(engine.InterpreterResult, "Native");
        }

        [Test]
        public void Engine_Compile_Recursive()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"fun Recur(a)
{
    if(a > 0) 
    {
        print a;
        Recur(a-1);
    }
}

Recur(5);");

            Assert.AreEqual(engine.InterpreterResult, "54321");
        }

        [Test]
        public void Engine_Closure_Inner_Outer()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
var x = ""global"";
fun outer() {
    var x = ""outer"";
    fun inner()
    {
        print x;
    }
    inner();
}
outer(); ");

            Assert.AreEqual(engine.InterpreterResult, "outer");
        }

        [Test]
        public void Engine_Closure_Tripup()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
fun outer() {
  var x = ""value"";
  fun middle() {
    fun inner() {
      print x;
    }

    print ""create inner closure"";
    return inner;
  }

  print ""return from outer"";
  return middle;
}

var mid = outer();
var in = mid();
in();");

            Assert.AreEqual(engine.InterpreterResult, @"return from outer
create inner closure
value");
        }

        [Test]
        public void Engine_Closure_StillOnStack()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
fun outer() {
  var x = ""outside"";
  fun inner() {
        print x;
    }
    inner();
}
outer();");

            Assert.AreEqual(engine.InterpreterResult, "outside");
        }

        [Test]
        public void Engine_Closure_ExampleDissasembly()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
fun outer() {
  var a = 1;
  var b = 2;
  fun middle() {
    var c = 3;
    var d = 4;
    fun inner() {
      print a + c + b + d;
    }
  }
}");
        }

        [Test]
        public void Engine_Closure_Counter()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
fun makeCounter() {
  var i = 0;
  fun count() {
    i = i + 1;
    print i;
  }

  return count;
}

var c1 = makeCounter();

c1();
c1();

var c2 = makeCounter();
c2();
c2();");

            Assert.AreEqual(engine.InterpreterResult, "1212");
        }

        [Test]
        public void Engine_Class_Empty()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
class Brioche {}
print Brioche;");

            Assert.AreEqual(engine.InterpreterResult, "<class Brioche>");
        }

        [Test]
        public void Engine_Class_Instance_Empty()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
class Brioche {}
print Brioche();");

            Assert.AreEqual(engine.InterpreterResult, "<inst Brioche>");
        }

        [Test]
        public void Engine_Class_Instance_Simple0()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
class Toast {}
Toast().a = 3;");

            Assert.AreEqual(engine.InterpreterResult, "");
        }

        [Test]
        public void Engine_Class_Instance_Simple1()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
class Toast {}
var toast = Toast();
print toast.jam = ""grape"";");

            Assert.AreEqual(engine.InterpreterResult, "grape");
        }

        [Test]
        public void Engine_Class_Instance_Simple2()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
class Pair {}

var pair = Pair();
pair.first = 1;
pair.second = 2;
print pair.first + pair.second;");

            Assert.AreEqual(engine.InterpreterResult, "3");
        }

        [Test]
        public void Engine_Class_Method_Simple1()
        {
            var engine = new ByteCodeLoxEngine();

            engine.Run(@"
class T 
{
    Say(){print 7;}
}

var t = T();
t.Say();");

            Assert.AreEqual(engine.InterpreterResult, "7");
        }

    }
    //todo functions aren't getting assigned to the globals the way we expect

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
        public VM VM => _vm;

        protected void AppendResult(string str) => InterpreterResult += str;
        public virtual void Run(string testString)
        {
            try
            {
                var tokens = _scanner.Scan(testString);
                var chunk = _compiler.Compile(tokens);
                _disasembler.DoChunk(chunk);
                _vm.Interpret(chunk);
            }
            catch (LoxException e)
            {
                AppendResult(e.Message);
            }
            finally
            {
                Debug.Log(InterpreterResult);
                Debug.Log(Disassembly);
                Debug.Log(_vm.GenerateGlobalsDump());
            }
        }
    }
}