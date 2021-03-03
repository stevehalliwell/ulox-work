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
  { { [ call [ 1:48 - IDENTIFIER print ][ ( [ 1:50 - IDENTIFIER i ] ) ] ] } }
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
  fun 7:16 - IDENTIFIER Mul[  ( 7:18 - IDENTIFIER l | 7:22 - IDENTIFIER r ) { return [ ( [ 7:36 - STAR * [ 7:35 - IDENTIFIER l ] [ 7:37 - IDENTIFIER r ] ] ) ] } ] }
  { return [ ( [ 8:27 - SLASH / [ call [ 8:19 - IDENTIFIER Mul ][ ( [ 8:21 - IDENTIFIER a ][ 8:23 - IDENTIFIER b ] ) ] ] [ 8:30 - IDENTIFIER c ] ] ) ] } ] }
{ var 11:8 - IDENTIFIER res[ call [ 11:22 - IDENTIFIER ComboMath ][ ( [ 11:26 - IDENTIFIER foo ][ 11:32 - IDENTIFIER bar ][ 11:38 - IDENTIFIER car ] ) ] ] }")
                .SetName("MathAndFunctions");

            yield return new TestCaseData(
@"class Simple
{
}

var s = Simple();
s.a = 1;",
@"{ class 1:13 - IDENTIFIER Simple }
{ var 5:6 - IDENTIFIER s[ call [ 5:17 - IDENTIFIER Simple ][  ] ] }
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
{ [ assign 9:1 - IDENTIFIER a[ 9:10 - MINUS - [ 9:7 - IDENTIFIER a ] [ ( [ 9:17 - PLUS + [ 1 ] [ 2 ] ] ) ] ] ] }
{ [ assign 10:1 - IDENTIFIER a[ 10:5 - MINUS - [ 10:1 - IDENTIFIER a ] [ 10:11 - PLUS + [ 1 ] [ 2 ] ] ] ] }
{ [ assign 11:1 - IDENTIFIER a[ 11:5 - MINUS - [ 11:1 - IDENTIFIER a ] [ ( [ 11:12 - PLUS + [ 1 ] [ 2 ] ] ) ] ] ] }")
                .SetName("CompoundAssign");

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
   instance { var 1:18 - IDENTIFIER a }{ var 1:20 - IDENTIFIER b }{ var 1:22 - IDENTIFIER c }{ 
  fun 1:11 - IDENTIFIER init[  ( 0:0 - IDENTIFIER self )  ] } }")
                .SetName("MultiFieldDeclare");

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
