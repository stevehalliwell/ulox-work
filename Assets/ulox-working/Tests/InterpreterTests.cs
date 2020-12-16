using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


namespace ULox.Tests
{
    public class InterpreterTests
    {
        private class TestLoxEngine
        {
            public string InterpreterResult { get; private set; }
            private LoxEngine loxEngine;

            void SetResult(string str) => InterpreterResult += str;

            public TestLoxEngine()
            {
                var interp = new Interpreter(SetResult);
                loxEngine = new LoxEngine(
                    new Scanner(SetResult),
                    new Parser(),
                    new Resolver(interp),
                    interp,
                    SetResult);
            }

            public void Run(string testString)
            {
                loxEngine.Run(testString);
            }
        }

        public static IEnumerable<TestCaseData> Generator()
        {
            yield return new TestCaseData(
@"print 1 + 4 * 2 / 2;",
@"5")
                .SetName("Math_Expr");

            yield return new TestCaseData(
@"print 4 * (2 / 2 + 1);",
@"8")
                .SetName("Math_Grouping_Expr");

            yield return new TestCaseData(
@"if( (1 > 2 and 4 < 5) or true)
{
    print ""inner"";
}",
@"inner")
                .SetName("Logic_Expr");

            yield return new TestCaseData(
@"var a = false;
var b = ""Hello"";

print a or b; ",
@"Hello")
                .SetName("Print_Logic");

            yield return new TestCaseData(
@"var res;
var a = ""global a"";
var b = ""global b"";
var c = ""global c"";
{
    var a = ""outer a"";
    var b = ""outer b"";
    {
        var a = ""inner a"";
        res = res + a;
        res = res + b;
        res = res + c;
    }
    res = res + a;
    res = res + b;
    res = res + c;
}
res = res + a;
res = res + b;
res = res + c;
print res;",
@"inner aouter bglobal couter aouter bglobal cglobal aglobal bglobal c")
                .SetName("Globals_Locals");

            yield return new TestCaseData(
@"var a = 0;
var temp;

for (var b = 1; a < 10000; b = temp + b)
{
    temp = a;
    a = b;
}

print a;",
@"10946")
                .SetName("Loop_Fib");

            yield return new TestCaseData(
@"fun sayHi(first, last)
{
    print ""Hi, "" + first + "" "" + last + ""!"";
}

sayHi(""Dear"", ""Reader"");",
@"Hi, Dear Reader!")
                .SetName("Func_Params");

            yield return new TestCaseData(
@"fun fib(n)
{
    if (n <= 1) { return n; }
    return fib(n - 2) + fib(n - 1);
}

var i = 0;
var last;
for (; i < 20; i = i + 1)
{
    last = fib(i);
}

print last;",
@"4181")
                .SetName("Recursive_Fib");

            yield return new TestCaseData(
@"fun makeCounter()
{
    var i = 0;
    fun count()
    {
        i = i + 1;
        return i;
    }

    return count;
}

var counter = makeCounter();
counter();
counter();
print counter();",
@"3")
                .SetName("Closure_Counter");

            yield return new TestCaseData(
@"class Bagel {}
var bagel = Bagel();
print bagel;",
@"<inst Bagel>")
                .SetName("Class_Empty");

            yield return new TestCaseData(
@"class PartialClass 
{
}

var partial = PartialClass();

partial.BeginingOfStatement = ""Hello "";
partial.EndOfStatement = ""World"";

print partial.BeginingOfStatement + partial.EndOfStatement; ",
@"Hello World")
                .SetName("Class_DataStore");

            yield return new TestCaseData(
@"
class Bacon {
  eat() {
    print ""Crunch crunch crunch!"";
  }
}

Bacon().eat();",
@"Crunch crunch crunch!")
                .SetName("Class_Method");

            yield return new TestCaseData(
@"class Cake {
  taste() {
    var adjective = ""delicious"";
    print ""The "" + this.flavor + "" cake is "" + adjective + ""!"";
  }
}

var cake = Cake();
cake.flavor = ""German chocolate"";
cake.taste();",
@"The German chocolate cake is delicious!")
                .SetName("Class_thisScope");

            yield return new TestCaseData(
@"class Circle {
  init(radius) {
    this.radius = radius;
  }

  area(){return 3.14 * this.radius * this.radius;}
}

var circ = Circle(4);
print circ.area();",
@"50.24")
                .SetName("Class_init");

            yield return new TestCaseData(
@"class Klass {}
var k = Klass();
fun Func(){}
var i = 1;
var f = 1.01;
var s = ""hello"";

print Klass;
print k;
print Func;
print i;
print f;
print s;",
@"<class Klass><inst Klass><fn Func>11.01hello")
                .SetName("Print_Types");

            yield return new TestCaseData(
@"class Doughnut {
  cook() {
    print ""Fry until golden brown."";
  }
}

class BostonCream < Doughnut {
  cook() {
    super.cook();
    print ""Pipe full of custard and coat with chocolate."";
  }
}

BostonCream().cook();",
@"Fry until golden brown.Pipe full of custard and coat with chocolate.")
                .SetName("Class_Super");
        }

    [Test]
        [TestCaseSource(nameof(Generator))]
        public void Interpreter_StringifiedResult_Matches(string testString, string requiredResult)
        {
            var engine = new TestLoxEngine();

            engine.Run(testString);

            Assert.AreEqual(requiredResult, engine.InterpreterResult);
        }
    }
}


