using NUnit.Framework;
using System.Collections.Generic;

namespace ULox.Tests
{
    public class ParserTests
    {
        public static IEnumerable<TestCaseData> Generator()
        {
            yield return new TestCaseData(
@"for(var i = 0; i < 10; i = i+1) {print(i);}",
@"{ { var 1:10 - IDENTIFIER i[ 0 ] }
{ while [ 1:23 - LESS < [ 1:20 - IDENTIFIER i ] [ 10 ] ]
  { { [ call [ 1:48 - IDENTIFIER print ]( [ 1:50 - IDENTIFIER i ] ) ] } }
  { [ assign 1:31 - IDENTIFIER i[ 1:38 - PLUS + [ 1:37 - IDENTIFIER i ] [ 1 ] ] ] } } }")
                .SetName("ForLoop");
            yield return new TestCaseData(
@"var a = 1;",
@"{ var 1:6 - IDENTIFIER a[ 1 ] }")
                .SetName("VarDecl");
            yield return new TestCaseData(
@"var foo = 1;
var bar = 2;
var car = 3;

fun ComboMath(a,b,c)
{
    fun Mul(l, r) {return l*r;}
    return Mul(a,b) / c;
}

var res = ComboMath(foo, bar, car);",
@"{ var 1:8 - IDENTIFIER foo[ 1 ] }
{ var 2:8 - IDENTIFIER bar[ 2 ] }
{ var 3:8 - IDENTIFIER car[ 3 ] }
{ fun 5:14 - IDENTIFIER ComboMath[  ( 5:16 - IDENTIFIER a | 5:18 - IDENTIFIER b | 5:20 - IDENTIFIER c ) { 
  fun 7:16 - IDENTIFIER Mul[  ( 7:18 - IDENTIFIER l | 7:22 - IDENTIFIER r ) { return [ 7:36 - STAR * [ 7:35 - IDENTIFIER l ] [ 7:37 - IDENTIFIER r ] ] } ] }
  { return [ 8:27 - SLASH / [ call [ 8:19 - IDENTIFIER Mul ]( [ 8:21 - IDENTIFIER a ][ 8:23 - IDENTIFIER b ] ) ] [ 8:30 - IDENTIFIER c ] ] } ] }
{ var 11:8 - IDENTIFIER res[ call [ 11:22 - IDENTIFIER ComboMath ]( [ 11:26 - IDENTIFIER foo ][ 11:32 - IDENTIFIER bar ][ 11:38 - IDENTIFIER car ] ) ] }")
                .SetName("MathAndFunctions");
            yield return new TestCaseData(
@"class Foo
{
    init()
{
this.a = 1;
}
}

/*

class Bar < Foo
{
init()
{
super.init();
this.b = 2;
}
}
*/

var inst = Foo();

print( inst.a);",
@"{ class 1:10 - IDENTIFIER Foo
   instance 
  fun 3:12 - IDENTIFIER init[ { [ [ 5:4 - THIS this ]5:6 - IDENTIFIER a[ 1 ] ] } ] }
{ var 11:9 - IDENTIFIER inst[ call [ 11:17 - IDENTIFIER Foo ] ] }
{ [ call [ 13:5 - IDENTIFIER print ]( [ 13:14 - IDENTIFIER a[ 13:12 - IDENTIFIER inst ] ] ) ] }")
                .SetName("Class");
            yield return new TestCaseData(
@"class Foo
{
    init()
{
this.a = 1;
}
}

class Bar < Foo
{
init()
{
super.init();
this.b = 2;
}
}

var inst = Bar();

print (inst.a + inst.b);",
@"{ class 1:10 - IDENTIFIER Foo
   instance 
  fun 3:12 - IDENTIFIER init[ { [ [ 5:4 - THIS this ]5:6 - IDENTIFIER a[ 1 ] ] } ] }
{ class 9:10 - IDENTIFIER Bar inherit [ 9:18 - IDENTIFIER Foo ]
   instance 
  fun 11:4 - IDENTIFIER init[ { [ call [ super 13:10 - IDENTIFIER init ] ] }
    { [ [ 14:4 - THIS this ]14:6 - IDENTIFIER b[ 2 ] ] } ] }
{ var 18:9 - IDENTIFIER inst[ call [ 18:17 - IDENTIFIER Bar ] ] }
{ [ call [ 20:5 - IDENTIFIER print ]( [ 20:17 - PLUS + [ 20:14 - IDENTIFIER a[ 20:12 - IDENTIFIER inst ] ] [ 20:25 - IDENTIFIER b[ 20:23 - IDENTIFIER inst ] ] ] ) ] }")
                .SetName("Inher");
            yield return new TestCaseData(
@"var logic = true and false or true;
var comparison = 1 < 2 and 2 >= 3 or 1 > 2 and 2 <= 3;

class WithInit{
    init(a,b,c)
    {
        this.a = a;
        this.b = b;
        this.c = c;
    }
}

var inst = WithInit(!logic,comparison,3);",
@"{ var 1:10 - IDENTIFIER logic[ 1:35 - OR or [ 1:24 - AND and [ True ][ False ] ][ True ] ] }
{ var 2:15 - IDENTIFIER comparison[ 2:46 - OR or [ 2:32 - AND and [ 2:24 - LESS < [ 1 ] [ 2 ] ][ 2:39 - GREATER_EQUAL > [ 2 ] [ 3 ] ] ][ 2:60 - AND and [ 2:52 - GREATER > [ 1 ] [ 2 ] ][ 2:67 - LESS_EQUAL < [ 2 ] [ 3 ] ] ] ] }
{ class 4:15 - IDENTIFIER WithInit
   instance 
  fun 5:12 - IDENTIFIER init[  ( 5:14 - IDENTIFIER a | 5:16 - IDENTIFIER b | 5:18 - IDENTIFIER c ) { [ [ 7:20 - THIS this ]7:22 - IDENTIFIER a[ 7:28 - IDENTIFIER a ] ] }
    { [ [ 8:20 - THIS this ]8:22 - IDENTIFIER b[ 8:28 - IDENTIFIER b ] ] }
    { [ [ 9:20 - THIS this ]9:22 - IDENTIFIER c[ 9:28 - IDENTIFIER c ] ] } ] }
{ var 13:9 - IDENTIFIER inst[ call [ 13:22 - IDENTIFIER WithInit ]( [ 13:24 - BANG ![ 13:29 - IDENTIFIER logic ] ][ 13:40 - IDENTIFIER comparison ][ 3 ] ) ] }")
                .SetName("LogicAndInitClass");


            yield return new TestCaseData(
@"class Simple
{
}

var s = Simple();
s.a = 1;",
@"{ class 1:13 - IDENTIFIER Simple }
{ var 5:6 - IDENTIFIER s[ call [ 5:17 - IDENTIFIER Simple ] ] }
{ [ [ 6:1 - IDENTIFIER s ]6:3 - IDENTIFIER a[ 1 ] ] }")
                .SetName("SimpleClass");


            yield return new TestCaseData(
@"var a = 10;
while(a > 0) {a = a - 1;}",
@"{ var 1:6 - IDENTIFIER a[ 10 ] }
{ while [ 2:10 - GREATER > [ 2:7 - IDENTIFIER a ] [ 0 ] ]
  { { [ assign 2:18 - IDENTIFIER a[ 2:27 - MINUS - [ 2:24 - IDENTIFIER a ] [ 1 ] ] ] } } }")
                .SetName("While");


            yield return new TestCaseData(
@"var a = 1 < 2 ? 3 : 4;",
@"{ var 1:6 - IDENTIFIER a[ cond [ 1:15 - LESS < [ 1 ] [ 2 ] ] ? [ 3 ] : [ 4 ] ] }")
                .SetName("Conditional");


            yield return new TestCaseData(
@"
var a = 0;
a = a + 1;
a += 1;
a = a + 1 + 2;
a += 1 + 2;
a = a - 1;
a -= 1;
a = a - (1 + 2);
a -= 1 + 2;
a -= (1 + 2);",
@"{ var 2:6 - IDENTIFIER a[ 0 ] }
{ [ assign 3:1 - IDENTIFIER a[ 3:10 - PLUS + [ 3:7 - IDENTIFIER a ] [ 1 ] ] ] }
{ [ assign 4:1 - IDENTIFIER a[ 4:5 - PLUS + [ 4:1 - IDENTIFIER a ] [ 1 ] ] ] }
{ [ assign 5:1 - IDENTIFIER a[ 5:16 - PLUS + [ 5:10 - PLUS + [ 5:7 - IDENTIFIER a ] [ 1 ] ] [ 2 ] ] ] }
{ [ assign 6:1 - IDENTIFIER a[ 6:5 - PLUS + [ 6:1 - IDENTIFIER a ] [ 6:11 - PLUS + [ 1 ] [ 2 ] ] ] ] }
{ [ assign 7:1 - IDENTIFIER a[ 7:10 - MINUS - [ 7:7 - IDENTIFIER a ] [ 1 ] ] ] }
{ [ assign 8:1 - IDENTIFIER a[ 8:5 - MINUS - [ 8:1 - IDENTIFIER a ] [ 1 ] ] ] }
{ [ assign 9:1 - IDENTIFIER a[ 9:10 - MINUS - [ 9:7 - IDENTIFIER a ] [ [ 9:17 - PLUS + [ 1 ] [ 2 ] ] ] ] ] }
{ [ assign 10:1 - IDENTIFIER a[ 10:5 - MINUS - [ 10:1 - IDENTIFIER a ] [ 10:11 - PLUS + [ 1 ] [ 2 ] ] ] ] }
{ [ assign 11:1 - IDENTIFIER a[ 11:5 - MINUS - [ 11:1 - IDENTIFIER a ] [ [ 11:12 - PLUS + [ 1 ] [ 2 ] ] ] ] ] }")
                .SetName("CompoundAssign");


            yield return new TestCaseData(
@"class Test{init(){this.a = 0;}}
var t = Test();

t.a = t.a + 1;
t.a += 1;",
@"{ class 1:11 - IDENTIFIER Test
   instance 
  fun 1:16 - IDENTIFIER init[ { [ [ 1:23 - THIS this ]1:25 - IDENTIFIER a[ 0 ] ] } ] }
{ var 2:6 - IDENTIFIER t[ call [ 2:15 - IDENTIFIER Test ] ] }
{ [ [ 4:1 - IDENTIFIER t ]4:3 - IDENTIFIER a[ 4:14 - PLUS + [ 4:11 - IDENTIFIER a[ 4:9 - IDENTIFIER t ] ] [ 1 ] ] ] }
{ [ [ 5:1 - IDENTIFIER t ]5:3 - IDENTIFIER a[ 5:7 - PLUS + [ 5:3 - IDENTIFIER a[ 5:1 - IDENTIFIER t ] ] [ 1 ] ] ] }")
                .SetName("CompoundAssignClasses");

            yield return new TestCaseData(
@"class Test
{
    init(){this.a = 1;}
    Geta(){return this.a;}
    Seta(value){this.a = value;}
}",
@"{ class 1:11 - IDENTIFIER Test
   instance 
  fun 3:12 - IDENTIFIER init[ { [ [ 3:19 - THIS this ]3:21 - IDENTIFIER a[ 1 ] ] } ]
  fun 4:12 - IDENTIFIER Geta[ { return [ 4:29 - IDENTIFIER a[ 4:27 - THIS this ] ] } ]
  fun 5:12 - IDENTIFIER Seta[  ( 5:18 - IDENTIFIER value ) { [ [ 5:24 - THIS this ]5:26 - IDENTIFIER a[ 5:36 - IDENTIFIER value ] ] } ] }")
                .SetName("ManualClassGetSet");

            yield return new TestCaseData(
@"class Test
{
    get a;
}",
@"{ class 1:11 - IDENTIFIER Test
   instance { var 3:14 - IDENTIFIER _a }
  fun 3:14 - IDENTIFIER a[ { return [ 3:14 - IDENTIFIER _a[ 1:11 - THIS this ] ] } ] }")
                .SetName("AutoClassGetAndSet");

            yield return new TestCaseData(
@"class Test
{
    getset a;
}",
@"{ class 1:11 - IDENTIFIER Test
   instance { var 3:17 - IDENTIFIER _a }
  fun 3:17 - IDENTIFIER a[ { return [ 3:17 - IDENTIFIER _a[ 1:11 - THIS this ] ] } ]
  fun 3:17 - IDENTIFIER Seta[  ( 3:17 - IDENTIFIER value ) { [ [ 1:11 - THIS this ]3:17 - IDENTIFIER _a[ 3:17 - IDENTIFIER value ] ] } ] }")
                .SetName("AutoClassGetSet");

            yield return new TestCaseData(
@"var a,b,c;",
@"{ { var 1:6 - IDENTIFIER a }{ { var 1:8 - IDENTIFIER b }{ var 1:10 - IDENTIFIER c } } }")
                .SetName("MultiVariableDeclare");

            yield return new TestCaseData(
@"var a = 1,b = true,c = ""three"";",
@"{ { var 1:6 - IDENTIFIER a[ 1 ] }{ { var 1:14 - IDENTIFIER b[ True ] }{ var 1:25 - IDENTIFIER c[ three ] } } }")
                .SetName("MultiVariableDeclareAndInit");

            yield return new TestCaseData(
@"class Test{var a,b,c;}",
@"{ class 1:11 - IDENTIFIER Test
   instance { var 1:18 - IDENTIFIER a }{ var 1:20 - IDENTIFIER b }{ var 1:22 - IDENTIFIER c } }")
                .SetName("MultiFieldDeclare");

            yield return new TestCaseData(
@"
class Test
{
    init(a, b) { this._a = a; print (b); }
    get a = true;
    set d = 7;
    getset i, j, k;
    func(funky) { d(funky); }
}",
@"{ class 2:11 - IDENTIFIER Test
   instance { var 5:14 - IDENTIFIER _a[ True ] }{ var 6:14 - IDENTIFIER _d[ 7 ] }{ var 7:17 - IDENTIFIER _i }{ var 7:21 - IDENTIFIER _j }{ var 7:25 - IDENTIFIER _k }
  fun 4:12 - IDENTIFIER init[  ( 4:14 - IDENTIFIER a | 4:18 - IDENTIFIER b ) { [ [ 4:28 - THIS this ]4:31 - IDENTIFIER _a[ 4:37 - IDENTIFIER a ] ] }
    { [ call [ 4:45 - IDENTIFIER print ]( [ 4:49 - IDENTIFIER b ] ) ] } ]
  fun 5:14 - IDENTIFIER a[ { return [ 5:14 - IDENTIFIER _a[ 2:11 - THIS this ] ] } ]
  fun 6:14 - IDENTIFIER Setd[  ( 6:14 - IDENTIFIER value ) { [ [ 2:11 - THIS this ]6:14 - IDENTIFIER _d[ 6:14 - IDENTIFIER value ] ] } ]
  fun 7:17 - IDENTIFIER i[ { return [ 7:17 - IDENTIFIER _i[ 2:11 - THIS this ] ] } ]
  fun 7:17 - IDENTIFIER Seti[  ( 7:17 - IDENTIFIER value ) { [ [ 2:11 - THIS this ]7:17 - IDENTIFIER _i[ 7:17 - IDENTIFIER value ] ] } ]
  fun 7:21 - IDENTIFIER j[ { return [ 7:21 - IDENTIFIER _j[ 2:11 - THIS this ] ] } ]
  fun 7:21 - IDENTIFIER Setj[  ( 7:21 - IDENTIFIER value ) { [ [ 2:11 - THIS this ]7:21 - IDENTIFIER _j[ 7:21 - IDENTIFIER value ] ] } ]
  fun 7:25 - IDENTIFIER k[ { return [ 7:25 - IDENTIFIER _k[ 2:11 - THIS this ] ] } ]
  fun 7:25 - IDENTIFIER Setk[  ( 7:25 - IDENTIFIER value ) { [ [ 2:11 - THIS this ]7:25 - IDENTIFIER _k[ 7:25 - IDENTIFIER value ] ] } ]
  fun 8:12 - IDENTIFIER func[  ( 8:18 - IDENTIFIER funky ) { [ call [ 8:25 - IDENTIFIER d ]( [ 8:31 - IDENTIFIER funky ] ) ] } ] }")
                .SetName("ClassSugar");

            yield return new TestCaseData(
@"",
@"")
                .SetName("Empty");
        }

        [Test]
        [TestCaseSource(nameof(Generator))]
        public void Parser_MatchesPrinter(string testString, string requiredAST)
        {
            var scanner = new Scanner();
            scanner.Scan(testString);

            var parser = new Parser();
            var res = parser.Parse(scanner.Tokens);

            var printer = new ASTPrinter();
            printer.Print(res);

            var resultingString = printer.FinalString;

            Assert.AreEqual(resultingString, requiredAST);
        }
    }
}
