﻿using NUnit.Framework;
using System.Collections.Generic;

namespace ULox.Tests
{
    public class ParserTests
    {
        public static IEnumerable<TestCaseData> Generator()
        {
            yield return new TestCaseData(
@"for(var i = 0; i < 10; i = i+1) {print i;}",
@"{ { var 0:10 - IDENTIFIER i[ 0 ] }
{ while [ 0:23 - LESS < [ 0:20 - IDENTIFIER i ] [ 10 ] ]
  { { { print [ 0:51 - IDENTIFIER i ] } }
  { [ assign 0:31 - IDENTIFIER i[ 0:38 - PLUS + [ 0:37 - IDENTIFIER i ] [ 1 ] ] ] } } } }")
                .SetName("ForLoop");
            yield return new TestCaseData(
@"var a = 1;",
@"{ var 0:6 - IDENTIFIER a[ 1 ] }")
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
@"{ var 0:8 - IDENTIFIER foo[ 1 ] }
{ var 1:8 - IDENTIFIER bar[ 2 ] }
{ var 2:8 - IDENTIFIER car[ 3 ] }
{ fun 4:14 - IDENTIFIER ComboMath ( 4:16 - IDENTIFIER a | 4:18 - IDENTIFIER b | 4:20 - IDENTIFIER c ) 
  { fun 6:16 - IDENTIFIER Mul ( 6:18 - IDENTIFIER l | 6:22 - IDENTIFIER r ) 
    { return [ 6:36 - STAR * [ 6:35 - IDENTIFIER l ] [ 6:37 - IDENTIFIER r ] ] } }
  { return [ 7:27 - SLASH / [ call [ 7:19 - IDENTIFIER Mul ]( [ 7:21 - IDENTIFIER a ][ 7:23 - IDENTIFIER b ] ) ] [ 7:30 - IDENTIFIER c ] ] } }
{ var 10:8 - IDENTIFIER res[ call [ 10:22 - IDENTIFIER ComboMath ]( [ 10:26 - IDENTIFIER foo ][ 10:32 - IDENTIFIER bar ][ 10:38 - IDENTIFIER car ] ) ] }")
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

print inst.a;",
@"{ class 0:10 - IDENTIFIER Foo
  fun 2:12 - IDENTIFIER init
    { [ [ 4:4 - THIS this ]4:6 - IDENTIFIER a[ 1 ] ] } }
{ var 10:9 - IDENTIFIER inst[ call [ 10:17 - IDENTIFIER Foo ] ] }
{ print [ 12:13 - IDENTIFIER a[ 12:11 - IDENTIFIER inst ] ] }")
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

print inst.a + inst.b;",
@"{ class 0:10 - IDENTIFIER Foo
  fun 2:12 - IDENTIFIER init
    { [ [ 4:4 - THIS this ]4:6 - IDENTIFIER a[ 1 ] ] } }
{ class 8:10 - IDENTIFIER Bar inherit [ 8:18 - IDENTIFIER Foo ]
  fun 10:4 - IDENTIFIER init
    { [ call [ super 12:10 - IDENTIFIER init ] ] }
    { [ [ 13:4 - THIS this ]13:6 - IDENTIFIER b[ 2 ] ] } }
{ var 17:9 - IDENTIFIER inst[ call [ 17:17 - IDENTIFIER Bar ] ] }
{ print [ 19:16 - PLUS + [ 19:13 - IDENTIFIER a[ 19:11 - IDENTIFIER inst ] ] [ 19:24 - IDENTIFIER b[ 19:22 - IDENTIFIER inst ] ] ] }")
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
@"{ var 0:10 - IDENTIFIER logic[ 0:35 - OR or [ 0:24 - AND and [ True ][ False ] ][ True ] ] }
{ var 1:15 - IDENTIFIER comparison[ 1:46 - OR or [ 1:32 - AND and [ 1:24 - LESS < [ 1 ] [ 2 ] ][ 1:39 - GREATER_EQUAL > [ 2 ] [ 3 ] ] ][ 1:60 - AND and [ 1:52 - GREATER > [ 1 ] [ 2 ] ][ 1:67 - LESS_EQUAL < [ 2 ] [ 3 ] ] ] ] }
{ class 3:15 - IDENTIFIER WithInit
  fun 4:12 - IDENTIFIER init ( 4:14 - IDENTIFIER a | 4:16 - IDENTIFIER b | 4:18 - IDENTIFIER c ) 
    { [ [ 6:20 - THIS this ]6:22 - IDENTIFIER a[ 6:28 - IDENTIFIER a ] ] }
    { [ [ 7:20 - THIS this ]7:22 - IDENTIFIER b[ 7:28 - IDENTIFIER b ] ] }
    { [ [ 8:20 - THIS this ]8:22 - IDENTIFIER c[ 8:28 - IDENTIFIER c ] ] } }
{ var 12:9 - IDENTIFIER inst[ call [ 12:22 - IDENTIFIER WithInit ]( [ 12:24 - BANG ![ 12:29 - IDENTIFIER logic ] ][ 12:40 - IDENTIFIER comparison ][ 3 ] ) ] }")
                .SetName("LogicClasses");


            yield return new TestCaseData(
@"class Simple
{
}

var s = Simple();
s.a = 1;",
@"{ class 0:13 - IDENTIFIER Simple
   }
{ var 4:6 - IDENTIFIER s[ call [ 4:17 - IDENTIFIER Simple ] ] }
{ [ [ 5:1 - IDENTIFIER s ]5:3 - IDENTIFIER a[ 1 ] ] }")
                .SetName("SimpleClass");


            yield return new TestCaseData(
@"var a = 10;
while(a > 0) {a = a - 1;}",
@"{ var 0:6 - IDENTIFIER a[ 10 ] }
{ while [ 1:10 - GREATER > [ 1:7 - IDENTIFIER a ] [ 0 ] ]
  { { [ assign 1:18 - IDENTIFIER a[ 1:27 - MINUS - [ 1:24 - IDENTIFIER a ] [ 1 ] ] ] } } }")
                .SetName("While");


            yield return new TestCaseData(
@"var logic = true and false or true;
var comparison = 1 < 2 and ((2 >= 3 or 1 > 2) and 2 <= 3);

class WithInit
{
    init(a,b,c)
    {
        this.max = a;
        if(b > a)
        this.max = b;
        if(c > a and c > b and b != c)
        this.max = c;
    }
}

var inst = WithInit(!logic,comparison,3);",
@"{ var 0:10 - IDENTIFIER logic[ 0:35 - OR or [ 0:24 - AND and [ True ][ False ] ][ True ] ] }
{ var 1:15 - IDENTIFIER comparison[ 1:32 - AND and [ 1:24 - LESS < [ 1 ] [ 2 ] ][ [ 1:63 - AND and [ [ 1:48 - OR or [ 1:41 - GREATER_EQUAL > [ 2 ] [ 3 ] ][ 1:54 - GREATER > [ 1 ] [ 2 ] ] ] ][ 1:70 - LESS_EQUAL < [ 2 ] [ 3 ] ] ] ] ] }
{ class 3:15 - IDENTIFIER WithInit
  fun 5:12 - IDENTIFIER init ( 5:14 - IDENTIFIER a | 5:16 - IDENTIFIER b | 5:18 - IDENTIFIER c ) 
    { [ [ 7:20 - THIS this ]7:24 - IDENTIFIER max[ 7:30 - IDENTIFIER a ] ] }
    { if [ 8:23 - GREATER > [ 8:20 - IDENTIFIER b ] [ 8:26 - IDENTIFIER a ] ]then { [ [ 9:20 - THIS this ]9:24 - IDENTIFIER max[ 9:30 - IDENTIFIER b ] ] }else  }
    { if [ 10:45 - AND and [ 10:31 - AND and [ 10:23 - GREATER > [ 10:20 - IDENTIFIER c ] [ 10:26 - IDENTIFIER a ] ][ 10:37 - GREATER > [ 10:34 - IDENTIFIER c ] [ 10:40 - IDENTIFIER b ] ] ][ 10:52 - BANG_EQUAL ! [ 10:48 - IDENTIFIER b ] [ 10:55 - IDENTIFIER c ] ] ]then { [ [ 11:20 - THIS this ]11:24 - IDENTIFIER max[ 11:30 - IDENTIFIER c ] ] }else  } }
{ var 15:9 - IDENTIFIER inst[ call [ 15:22 - IDENTIFIER WithInit ]( [ 15:24 - BANG ![ 15:29 - IDENTIFIER logic ] ][ 15:40 - IDENTIFIER comparison ][ 3 ] ) ] }")
                .SetName("LogicClass");


            yield return new TestCaseData(
@"",
@"")
                .SetName("Empty");
        }

        [Test]
        [TestCaseSource(nameof(Generator))]
        public void Parser_MatchesPrinter(string testString, string requiredAST)
        {
            var scanner = new Scanner(null);
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
