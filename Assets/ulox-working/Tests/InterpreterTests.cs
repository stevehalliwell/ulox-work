using NUnit.Framework;
using System.Collections.Generic;

namespace ULox.Tests
{
    //todo more metafields and metamethods tests
    //todo should you be able to get to metas via this or super or class or meta keyword? or not?
    //todo fthis keyword to get access to the closure you are running in?
    public class InterpreterTests
    {
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

            yield return new TestCaseData(
@"var a = 10;
a = -a;
print a;",
@"-10")
                .SetName("Unary");

            yield return new TestCaseData(
@"print ""hello incomplete string;",
@"IDENTIFIER|1:32 Unterminated String")
                .SetName("IncompleteStringDeclare");

            yield return new TestCaseData(
@"@",
@"IDENTIFIER|1:1 Unexpected character '@'")
                .SetName("UnexpectedChar");

            yield return new TestCaseData(
@"
var i;
for(i = 0; i < 3; i = i + 1)
{
    print i;
}",
@"012")
                .SetName("ForExprInitialiser");

            yield return new TestCaseData(
@"
var a = 2;
if(a == 0)
{print 1;}
else if (a == 1)
{}
else
{print 3;}",
@"3")
                .SetName("IfElseChain");

            yield return new TestCaseData(
@"print a;",
@"IDENTIFIER|1:8 Undefined variable a")
                .SetName("UndefinedVar");

            yield return new TestCaseData(
@"var Func = 6;
print Func();",
@"CLOSE_PAREN|2:13 Can only call function types")
                .SetName("NotAFunc");

            yield return new TestCaseData(
@"class Undef
{
}

print Undef().a;",
@"IDENTIFIER|5:16 Undefined property 'a'.")
                .SetName("UndefinedProperty");

            yield return new TestCaseData(
@"var a = 1 + a;",
@"IDENTIFIER|1:18 Undefined variable a")
                .SetName("CannotReadDuringInitGlobal");

            yield return new TestCaseData(
@"fun Local()
{
    var a = 1 + a;
}",
@"IDENTIFIER|3:26 Can't read local variable in its own initializer.")
                .SetName("CannotReadDuringInitLocal");

            yield return new TestCaseData(
@"var a = 1;
return a;",
@"RETURN|2:6 Cannot return outside of a function.")
                .SetName("CannotReturnWhenNotWithinAFunc");

            yield return new TestCaseData(
@"class Initer
{
    init()
    {
        this.a = 1;
        return this.a;
    }
}",
@"RETURN|6:22 Cannot return a value from an initializer")
                .SetName("CannotReturnValueFromInit");

            yield return new TestCaseData(
@"var a = 1;
var a = 2;",
@"Environment value redinition not allowed. Requested a:9 collided.")
                .SetName("CannotHaveDuplicateGlobals");

            yield return new TestCaseData(
@"fun Local()
{
    var a = 1;
    var a = 2;
}",
@"IDENTIFIER|4:14 Already a variable with this name in this scope.")
                .SetName("CannotHaveDuplicateLocals");

            yield return new TestCaseData(
@"class Base {}
class Child < Child {}",
@"IDENTIFIER|2:22 A class can't inherit from itself.")
                .SetName("CannotSuperSelf");

            yield return new TestCaseData(
@"fun Thiser()
{
    this.a = 5;
}",
@"THIS|3:12 Cannot use 'this' outside of a class.")
                .SetName("CannotUseThisOutsideMethods");

            yield return new TestCaseData(
@"class Base {}
class Child {
    init()
    {
        super.init();
    }
}",
@"SUPER|5:21 Cannot use 'super' in a class with no superclass.")
                .SetName("CannotSuperWithoutBase");

            yield return new TestCaseData(
@"fun init()
{
    super.init();
}
",
@"SUPER|3:13 Cannot use 'super' outside of a class.")
                .SetName("CannotSuperOutsideClass");

            yield return new TestCaseData(
@"var a = print 7;",
@"PRINT|1:16 Expect expression.")
                .SetName("CannotRValueStatementsInAssign");

            yield return new TestCaseData(
@"var a = 1;

if(a != null)
{
    print a;
}",
@"1")
                .SetName("IfNotNull");

            yield return new TestCaseData(
@"var a = 1;

if(a >= 1)
{
    print a;
}",
@"1")
                .SetName("GreaterOrEqual");

            yield return new TestCaseData(
@"var a = ""Hello "";
var b;
var c;
var d = ""World"";

print (a+b)+c+d;",
@"Hello World")
                .SetName("StringNullConcats");

            yield return new TestCaseData(
@"fun A(){}
fun B(){}

var res = A+B;",
@"PLUS|4:15 Operands must be numbers or strings.")
                .SetName("CannotPlusFunctions");

            yield return new TestCaseData(
@"var a = ""hello"";
var b = !a;

print b;",
@"False")
                .SetName("NotNotTruthy");

            yield return new TestCaseData(
@"var a = ""hello"";
var b = 6;
var c = a - b;",
@"MINUS|3:15 Operands must be numbers.")
                .SetName("CannotSubtractNonNumbers");

            yield return new TestCaseData(
@"fun Func(){}
var a = -Func;",
@"MINUS|2:12 Operands must be numbers.")
                .SetName("CannotNegateNonNumbers");

            yield return new TestCaseData(
@"fun Func(a,b){}
Func(7);",
@"CLOSE_PAREN|2:7 Expected 2 args but got 1")
                .SetName("CannotCallFunctionWithIncorrectParamCount");

            yield return new TestCaseData(
@"fun Func(a,b){}
class Klass < Func{}",
@"IDENTIFIER|2:21 Superclass must be a class.")
                .SetName("SuperMustBeClass");

            yield return new TestCaseData(
@"fun Func(a,b){}

print Func.a;",
@"IDENTIFIER|3:13 Only instances have properties.")
                .SetName("OnlyInstHasGet");

            yield return new TestCaseData(
@"fun Func(a,b){}

Func.a = 67;",
@"IDENTIFIER|3:6 Only instances have fields.")
                .SetName("OnlyInstHasSet");

            yield return new TestCaseData(
@"class Doughnut {
  cook() {
    print ""Fry until golden brown."";
  }
}

class BostonCream < Doughnut {
  cook() {
    super.Missing();
    print ""Pipe full of custard and coat with chocolate."";
  }
}

BostonCream().cook();",
@"IDENTIFIER|9:21 Undefined property 'Missing'.")
                .SetName("Class_SuperMissingMethod");

            yield return new TestCaseData(
@"fun Say(a,b,c)
{
    print a + b + c;
}

Say(""Hello"","" "",""World!"");",
@"Hello World!")
                .SetName("CallFuncWithLiterals");

            yield return new TestCaseData(
@"var a = 1 < 2 ? 3 : 4;
print a;",
@"3")
                .SetName("SimpleConditional");

            yield return new TestCaseData(
@"var a = !(1 < 2) ? 3 : 4;
print a;",
@"4")
                .SetName("SimpleConditionalReverse");

            yield return new TestCaseData(
@"print + 5;",
@"PLUS|1:8 Missing left-had operand.")
                .SetName("MissingLHS");

            yield return new TestCaseData(
@"print ""Hello "" + 7;",
@"Hello 7")
                .SetName("AutoNumberToStringConcat");

            yield return new TestCaseData(
@"var a = 0;
while(a < 10)
{
    print a;
    if (a > 3) {break;}
    a = a + 1;
}",
@"01234")
                .SetName("WhileBreak");

            yield return new TestCaseData(
@"for(var i = 0; i < 10; i = i + 1)
{
    print i;
    if (i > 3) {break;}
}",
@"01234")
                .SetName("ForBreak");

            yield return new TestCaseData(
@"var a = 0;
while(a < 10)
{
    a = a + 1;
    if (a > 3) {continue;}
    print a;
}",
@"123")
                .SetName("WhileContinue");

            yield return new TestCaseData(
@"for(var i = 0; i < 10; i = i + 1)
{
    if (i > 3) {continue;}
    print i;
}",
@"0123")
                .SetName("ForContinue");

            yield return new TestCaseData(
@"fun thrice(fn)
{
    for (var i = 1; i <= 3; i = i + 1)
    {
        fn(i);
    }
}

thrice(fun(a) {
    print a;
});",
@"123")
                .SetName("Lambda");

            yield return new TestCaseData(
@"fun Func()
{
    var a = 1;
    break;
}",
@"BREAK|4:13 Cannot break when not within a loop.")
                .SetName("CannotBreakHere");

            yield return new TestCaseData(
@"fun Func()
{
    var a = 1;
    continue;
}",
@"CONTINUE|4:16 Cannot continue when not within a loop.")
                .SetName("CannotContinueHere");

            yield return new TestCaseData(
@"fun Func()
{
    var a = 1;
    var b = 2;
    print b;
}",
@"3:14 - IDENTIFIER a Local variable is never read.")
                .SetName("UnusedLocals");

            yield return new TestCaseData(
@"class Circle {
  init(radius) {
    this.radius = radius;
  }

  area {
    return 3.14 * this.radius * this.radius;
  }
}

var circle = Circle(4);
print circle.area;",
@"50.24")
                .SetName("ClassGetProperty");

            yield return new TestCaseData(
@"class Math {
  class square(n) {
    return n * n;
  }
}

print Math.square(3);",
@"9")
                .SetName("MetaClassMethods");

            yield return new TestCaseData(
@"class Math {
  class square(n) {
    return n * n;
  }
}

Math.pi = 3.14;
print Math.pi;",
@"3.14")
                .SetName("MetaClassFieldStorage");

            yield return new TestCaseData(
@"class Math {
    class var PI = 3.14;
    class square(n) {
        return n * n;
    }
}

print Math.PI;",
@"3.14")
                .SetName("MetaClassVar");

            yield return new TestCaseData(
@"var i = 0;
loop
{
    i = i + 1;
    print i;
    if(i >= 9)
    {
        break;
    }
}",
@"123456789")
                .SetName("Loop");

            yield return new TestCaseData(
@"//print ++5;
//print --5;
var a = 0;
a = a + 1;
a += 1;
a *= 3;
a -= 2;
a /= 2;
a %= 1;
print a;

class Test{init(){this.a = 0;}}
var t = Test();
t.a = t.a + 1;
t.a += 1;
t.a *= 3;
t.a -= 2;
t.a /= 2;
t.a %= 1;
print t.a;
//a *=3;
//print a;
//a -= 1;
//print a;
//a /= 2;
//print a;",
@"00")
                .SetName("AddedOperators");

            yield return new TestCaseData(
@"print 50 % 40 % 9;",
@"1")
                .SetName("Modulus");

            yield return new TestCaseData(
@"class Square
{
    getset Side;
    Area {return this.Side * this.Side;}
}
var sq = Square();
print sq.Side;
sq.SetSide(2);
print sq.Area;",
@"null4")
                .SetName("ClassGetSet");

            yield return new TestCaseData(
@"class Square
{
    getset Side;
    Area {return this.Side * this.Side;}
}
var sq = Square();
print sq.Arae;",
@"IDENTIFIER|7:14 Undefined property 'Arae'.")
                .SetName("ClassGetSet_Typo_Error");

            yield return new TestCaseData(
@"class Square
{
    var Side;
    Area {return this.Side * this.Side;}
}
var sq = Square();
print sq.Area;
sq.Side = 2;
print sq.Area;",
@"STAR|4:35 Operands must be numbers.")
                .SetName("ClassVars_nullfield");

            yield return new TestCaseData(
@"class Square
{
    var Side = 2;
    Area {return this.Side * this.Side;}
}

print Square().Area;",
@"4")
                .SetName("ClassVars_InitialValue");

            yield return new TestCaseData(
@"class Square
{
    get Side = 2;
    Area {return this.Side * this.Side;}
}
var sq = Square();
print sq.Side;
print sq.Area;",
@"24")
                .SetName("ClassVars_GetInitialValue");

            yield return new TestCaseData(
@"class Square
{
    set Side = 1;
    Area {return this._Side * this._Side;}
}
var sq = Square();
sq.SetSide(2);
print sq.Area;",
@"4")
                .SetName("ClassVars_SetInitialValue");

            yield return new TestCaseData(
@"class Square
{
    getset Side = 1;
    Area {return this.Side * this.Side;}
}
var sq = Square();
sq.SetSide(2);
print sq.Side;
print sq.Area;",
@"24")
                .SetName("ClassVars_GetSetInitialValue");

            yield return new TestCaseData(
@"class Square
{
    init() {this.side = 1;}
    get Side {print this.side; return this.side;}
    Area {return this.side * this.side;}
}

var sq = Square();
sq.Side;
print sq.Area;",
@"11")
                .SetName("Class_CustomGet");

            yield return new TestCaseData(
@"class Square
{
    init() {this.side = 1;}
    set Side {print value; this.side = value;}
    Area {return this.side * this.side;}
}

var sq = Square();
sq.Side(2);
print sq.Area;",
@"24")
                .SetName("Class_CustomSet");

            yield return new TestCaseData(
@"class Square
{
    init() {this.side = 1;}
    get Side(a) {return this.side;}
    Area {return this.side * this.side;}
}",
@"IDENTIFIER|1:13 Cannot have arguments to a Get.")
                .SetName("Class_NoGetParams");

            yield return new TestCaseData(
@"class Square
{
    init() {this.side = 1;}
    set Side(a) {this.side = a;}
    Area {return this.side * this.side;}
}",
@"IDENTIFIER|1:13 Cannot have arguments to a Set. A 'value' param is auto generated.")
                .SetName("Class_NoSetParams");

            yield return new TestCaseData(
@"var klass = class Square
{
    init() {this.side = 1;}
    set Side(a) {this.side = a;}
    Area {return this.side * this.side;}
}",
@"CLASS|1:20 Expect expression.")
                .SetName("CannotAssignVarToClass");

            yield return new TestCaseData(
@"var a1 = var a2 = 10;",
@"VAR|1:15 Expect expression.")
                .SetName("CannotAssignVarToVar");

            yield return new TestCaseData(
@"class Square
{
    get a;
    set a;
}",
@"IDENTIFIER|3:14 Classes cannot have fields of identical names. Found more than 1 _a in class Square.")
                .SetName("Class_DupFields");

            yield return new TestCaseData(
@"class Square
{
    var a;
    a(){}
}",
@"IDENTIFIER|4:9 Classes cannot have a field and a method of identical names. Found more than 1 a in class Square.")
                .SetName("Class_DupFieldnMethod");

            yield return new TestCaseData(
@"class Square
{
    class get a;
    class set a;
}",
@"IDENTIFIER|3:21 Classes cannot have metaFields of identical names. Found more than 1 _a in class Square.")
                .SetName("Class_Static_DupFields");

            yield return new TestCaseData(
@"class Square
{
    class var a;
    class a(){}
}",
@"IDENTIFIER|4:16 Classes cannot have a metaFields and a metaMethods of identical names. Found more than 1 a in class Square.")
                .SetName("Class_Static_DupFieldnMethod");

            yield return new TestCaseData(
@"class Square
{
     a(){}
    a(){}
}",
@"IDENTIFIER|3:11 Classes cannot have methods of identical names. Found more than 1 a in class Square.")
                .SetName("Class_DupMethods");

            yield return new TestCaseData(
@"class Square
{
    class a(){}
    class a(){}
}",
@"IDENTIFIER|3:16 Classes cannot have metaMethods of identical names. Found more than 1 a in class Square.")
                .SetName("Class_Static_DupMethods");

            yield return new TestCaseData(
@"print ""Hello\r\nWorld!"";",
@"Hello
World!")
                .SetName("Print_EscapedChars");

            yield return new TestCaseData(
@"var val = 1;
get a(){print val;}",
@"GET|2:3 Only expected withing class declaration.")
                .SetName("Unexpected_get");

            yield return new TestCaseData(
@"var a = ""Why "", b = ""Hello "", c = ""There"";
print a + b + c;",
@"Why Hello There")
                .SetName("MultiVariablePrintMatch");

            yield return new TestCaseData(
@"class TestClass
{
    var a = ""Why "", b = ""Hello "", c = ""There"";
    Say() { print this.a + this.b + this.c; }
}
TestClass().Say();",
@"Why Hello There")
                .SetName("MultiVarFieldPrintMatch");

            yield return new TestCaseData(
@"class TestClass
{
    getset a = ""Why "", b = ""Hello "", c = ""There"";
    Say() { print this.a + this.b + this.c; }
}
TestClass().Say();",
@"Why Hello There")
                .SetName("MultiGetSetFieldPrintMatch");

            yield return new TestCaseData(
@"var a = true;
if(a) print 7;
else
{
    print -7;
}",
@"7")
                .SetName("SingleStatementIf");

            yield return new TestCaseData(
@"var i = 0;
while (i < 10) i += 1;
print i;",
@"10")
                .SetName("SingleStatementWhile");

            yield return new TestCaseData(
@"class Base { var a = 1; }
class Derived < Base { var b = 2;}
var inst = Derived();
print inst.b + inst.a;",
@"3")
                .SetName("SuperFields");

            yield return new TestCaseData(
@"class Base { BaseMeth(b) {print b + ""Bar"";} }
class Derived < Base { var c = ""Foo"";}
var inst = Derived();
inst.BaseMeth(inst.c);",
@"FooBar")
                .SetName("SuperMethod");

            yield return new TestCaseData(
@"class Base { BaseMeth(b) {print b + ""Bar"";} }
class Derived < Base { var c = ""Foo"";}
var inst = Derived();
inst.BaseMeth(inst.c);",
@"FooBar")
                .SetName("SuperMethodsAndField");

            yield return new TestCaseData(
@"class Base { BaseMeth(b) {print b + ""Bar"";} }
class Derived < Base { ChildMeth(a) {print ""Well, ""; super.BaseMeth(a);}}
var inst = Derived();
inst.ChildMeth(""Foo"");",
@"Well, FooBar")
                .SetName("SuperMethodAndChildMethodField");

            yield return new TestCaseData(
@"class Base 
{
    BaseMeth(b) {print b + ""Bar"";} 
}
class Derived < Base 
{ 
    ChildMeth(a) { print ""Well, ""; super.BaseMeth(a + Derived.fb);}

    class var fb = ""Foobar? "";
}
var inst = Derived();
inst.ChildMeth(""is it "");",
@"Well, is it Foobar? Bar")
                .SetName("MetaFieldAndSuperMethod");

            yield return new TestCaseData(
@"class Base 
{
    var fb = ""Foobar? "";
}
class Derived < Base 
{ 
    class BaseMeth(b) {print b + ""Bar"";} 
    ChildMeth(a) {print ""Well, ""; Derived.BaseMeth(a + this.fb);}
}
var inst = Derived();
inst.ChildMeth(""is it "");",
@"Well, is it Foobar? Bar")
                .SetName("MetaMethodAndSuperField");

            yield return new TestCaseData(
@"class Base 
{
    getset foo = ""Foo"";
}
class Derived < Base 
{ 
    ChildMeth(a) 
    {
        print a; 
        super.Setfoo(""Foo"");
        print super.foo() + ""Bar"";
    }
}
var inst = Derived();
inst.ChildMeth(""is it "");",
@"is it FooBar")
                .SetName("DerivedSuperProperty");

            yield return new TestCaseData(
@"class Base 
{
    init(){this.foo=""Foo"";}
}
class Derived < Base 
{ 
    ChildMeth(a) 
    {
        print a; 
        this.bar = ""Bar"";
        //todo document this difference, perhaps super is an error if not on method?
        //  would expect to use super here perhaps  
        print this.foo + this.bar;
    }
}
var inst = Derived();
inst.ChildMeth(""is it "");",
@"is it FooBar")
                .SetName("DerivedSuperManualProperty");

            yield return new TestCaseData(
@"class Base 
{
    init(){this.foo=""Foo"";}
}
class Derived < Base 
{ 
    ChildMeth(a) 
    {
        this.bar = ""Bar"";
        //todo document this difference, perhaps super is an error if not on method?
        //  would expect to use super here perhaps  
        print super.foo + this.bar;
    }
}
var inst = Derived();
inst.ChildMeth(""is it "");",
@"IDENTIFIER|12:68 Could not find 'foo'via 'super'.")
                .SetName("CannotUseSuperOnField");

            yield return new TestCaseData(
@"
class BaseBase
{
    Foo(a) {print ""BaseBase"" + a;}
}
class Base < BaseBase
{
    Foo(a) {print ""Base"" + super.Foo(a);}
}
class Derived < Base 
{
    Foo(a) {print ""Derived"" + super.Foo(a);}
}
var inst = Derived();
inst.Foo(""hi"");",
@"BaseBasehiBaseDerived")
                .SetName("SuperChain");

            yield return new TestCaseData(
@"
class BaseBase
{
    Foo(a) {print ""BaseBase"" + a;}
}
class Base < BaseBase
{
    Foo(a) {print ""Base"" + super.Foo(a);}
}
class Derived < Base 
{
    Foo(a) {print ""Derived"" + super(BaseBase).Foo(a);}
}
var inst = Derived();
inst.Foo(""hi"");",
@"BaseBasehiDerived")
                .SetName("SuperJumpChain");

            yield return new TestCaseData(
@"
class BaseBase
{
    Foo(a) {print ""BaseBase"" + a;}
}
class Base < BaseBase
{
    Foo(a) {print ""Base"" + super.Foo(a);}
}
class Derived < Base 
{
    Foo(a) {print ""Derived"" + super(BassBase).Foo(a);}
}
var inst = Derived();
inst.Foo(""hi"");",
@"IDENTIFIER|12:52 Could not find parent class of name 'BassBase' via 'super'.")
                .SetName("TypoSuperJumpChain");

            yield return new TestCaseData(
@"print """";",
@"")
                .SetName("Empty");
        }

        [Test]
        [TestCaseSource(nameof(Generator))]
        public void Interpreter_StringifiedResult_Matches(string testString, string requiredResult)
        {
            var engine = new TestLoxEngine();

            engine.Run(testString, true);

            Assert.AreEqual(requiredResult, engine.InterpreterResult);
        }
    }
}
