using NUnit.Framework;
using System.Collections.Generic;

namespace ULox.Tests
{
    //todo init that does work and assigns that to instance vars
    //todo move creation to the class, have pass self to init
    //todo more metafields and metamethods tests?
    //todo more multi return tests
    public class InterpreterTests
    {
        public static IEnumerable<TestCaseData> Generator()
        {
            yield return new TestCaseData(
@"print(1 + 4 * 2 / 2);",
@"5")
                .SetName("Math_Expr");

            yield return new TestCaseData(
@"print(4 * (2 / 2 + 1));",
@"8")
                .SetName("Math_Grouping_Expr");

            yield return new TestCaseData(
@"if( (1 > 2 and 4 < 5) or true)
{
    print(""inner"");
}",
@"inner")
                .SetName("Logic_Expr");

            yield return new TestCaseData(
@"var a = false;
var b = ""Hello"";

print( a or b); ",
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
print(res);",
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

print( a);",
@"10946")
                .SetName("Loop_Fib");

            yield return new TestCaseData(
@"fun sayHi(first, last)
{
    print(""Hi, "" + first + "" "" + last + ""!"");
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

print(last);",
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
print(counter());",
@"3")
                .SetName("Closure_Counter");

            yield return new TestCaseData(
@"class Bagel {}
var bagel = Bagel();
print(bagel);",
@"<inst Bagel>")
                .SetName("Class_Empty");

            yield return new TestCaseData(
@"class PartialClass
{
}

var partial = PartialClass();

partial.BeginingOfStatement = ""Hello "";
partial.EndOfStatement = ""World"";

print(partial.BeginingOfStatement + partial.EndOfStatement); ",
@"Hello World")
                .SetName("Class_DataStore");

            yield return new TestCaseData(
@"class Bacon {
  eat() {
    print( ""Crunch crunch crunch!"");
  }
}

Bacon().eat();",
@"Crunch crunch crunch!")
                .SetName("Class_Method");

            yield return new TestCaseData(
@"class Cake {
  taste(self) {
    var adjective = ""delicious"";
    print(""The "" + self.flavor + "" cake is "" + adjective + ""!"");
  }
}

var cake = Cake();
cake.flavor = ""German chocolate"";
cake.taste(cake);",
@"The German chocolate cake is delicious!")
                .SetName("Class_selfScope");

            yield return new TestCaseData(
@"class Circle {
    var radius;
    init(self, radius) {}

    area(self){return 3.14 * self.radius * self.radius;}
}

var circ = Circle(4);
print(circ.area(circ));",
@"50.24")
                .SetName("Class_init");

            yield return new TestCaseData(
@"class Klass {}
var k = Klass();
fun Func(){}
var i = 1;
var f = 1.01;
var s = ""hello"";

print( Klass);
print( k);
print( Func);
print( i);
print( f);
print( s);",
@"<class Klass><inst Klass><fn Func>11.01hello")
                .SetName("Print_Types");

            yield return new TestCaseData(
@"class Doughnut {
  cook() {
    print(""Fry until golden brown."");
  }
}

class BostonCream < Doughnut {
    mycook(self) {
    self.cook();
    print(""Pipe full of custard and coat with chocolate."");
  }
}

var bc = BostonCream();
bc.mycook(bc);",
@"Fry until golden brown.Pipe full of custard and coat with chocolate.")
                .SetName("Child_Class_CallParentFunc");

            yield return new TestCaseData(
@"var a = 10;
a = -a;
print(a);",
@"-10")
                .SetName("Unary");

            yield return new TestCaseData(
@"print(""hello incomplete string);",
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
    print(i);
}",
@"012")
                .SetName("ForExprInitialiser");

            yield return new TestCaseData(
@"
var a = 2;
if(a == 0)
{print(1);}
else if (a == 1)
{}
else
{print(3);}",
@"3")
                .SetName("IfElseChain");

            yield return new TestCaseData(
@"print(a);",
@"IDENTIFIER|1:7 Undefined variable a")
                .SetName("UndefinedVar");

            yield return new TestCaseData(
@"var Func = 6;
print(Func());",
@"CLOSE_PAREN|2:12 Can only call function types")
                .SetName("NotAFunc");

            yield return new TestCaseData(
@"class Undef
{
}

print(Undef().a);",
@"IDENTIFIER|5:15 Undefined property 'a' on <inst Undef>.")
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
@"var a = 1;
var a = 2;",
@"Environment value redefinition not allowed. Requested a:10 collided.")
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
                .SetName("CannotInheritSelf");

            yield return new TestCaseData(
@"var a = if(7);",
@"IF|1:13 Expect expression.")
                .SetName("CannotRValueStatementsInAssign");

            yield return new TestCaseData(
@"var a = 1;

if(a != null)
{
    print(a);
}",
@"1")
                .SetName("IfNotNull");

            yield return new TestCaseData(
@"var a = 1;

if(a >= 1)
{
    print(a);
}",
@"1")
                .SetName("GreaterOrEqual");

            yield return new TestCaseData(
@"var a = ""Hello "";
var b;
var c;
var d = ""World"";

print((a+b)+c+d);",
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

print(b);",
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

print(Func.a);",
@"IDENTIFIER|3:12 Only instances have properties.")
                .SetName("OnlyInstHasGet");

            yield return new TestCaseData(
@"fun Func(a,b){}

Func.a = 67;",
@"IDENTIFIER|3:6 Only instances have fields.")
                .SetName("OnlyInstHasSet");

            yield return new TestCaseData(
@"fun Say(a,b,c)
{
    print(a + b + c);
}

Say(""Hello"","" "",""World!"");",
@"Hello World!")
                .SetName("CallFuncWithLiterals");

            yield return new TestCaseData(
@"var a = 1 < 2 ? 3 : 4;
print (a);",
@"3")
                .SetName("SimpleConditional");

            yield return new TestCaseData(
@"var a = !(1 < 2) ? 3 : 4;
print(a);",
@"4")
                .SetName("SimpleConditionalReverse");

            yield return new TestCaseData(
@"print(+ 5);",
@"PLUS|1:7 Missing left-had operand.")
                .SetName("MissingLHS");

            yield return new TestCaseData(
@"print(""Hello "" + 7);",
@"Hello 7")
                .SetName("AutoNumberToStringConcat");

            yield return new TestCaseData(
@"var a = 0;
while(a < 10)
{
    print(a);
    if (a > 3) {break;}
    a = a + 1;
}",
@"01234")
                .SetName("WhileBreak");

            yield return new TestCaseData(
@"for(var i = 0; i < 10; i = i + 1)
{
    print(i);
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
    print(a);
}",
@"123")
                .SetName("WhileContinue");

            yield return new TestCaseData(
@"for(var i = 0; i < 10; i = i + 1)
{
    if (i > 3) {continue;}
    print(i);
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
    print(a);
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
    print(b);
}",
@"3:14 - IDENTIFIER a Local variable is never read.")
                .SetName("UnusedLocals");

            yield return new TestCaseData(
@"class Math {
  class square(n) {
    return n * n;
  }
}

print( Math.square(3));",
@"9")
                .SetName("MetaClassMethods");

            yield return new TestCaseData(
@"class Math {
  class square(n) {
    return n * n;
  }
}

Math.pi = 3.14;
print(Math.pi);",
@"3.14")
                .SetName("MetaClassFieldStorage");

            yield return new TestCaseData(
@"class Math {
    class var PI = 3.14;
    class square(n) {
        return n * n;
    }
}

print(Math.PI);",
@"3.14")
                .SetName("MetaClassVar");

            yield return new TestCaseData(
@"var i = 0;
loop
{
    i = i + 1;
    print(i);
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
print(a);

class Test{var a=0;}
var t = Test();
t.a = 0;
t.a = t.a + 1;
t.a += 1;
t.a *= 3;
t.a -= 2;
t.a /= 2;
t.a %= 1;
print(t.a);
//a *=3;
//print a;
//a -= 1;
//print a;
//a /= 2;
//print a;",
@"00")
                .SetName("AddedOperators");

            yield return new TestCaseData(
@"print(50 % 40 % 9);",
@"1")
                .SetName("Modulus");

            yield return new TestCaseData(
@"class Square
{
    var Side;
    Area(self) {return self.Side * self.Side;}
}
var sq = Square();
print(sq.Side);
sq.Side = 2;
print(sq.Area(sq));",
@"null4")
                .SetName("Class_Var_Func");

            yield return new TestCaseData(
@"class Square
{
    var Side;
    Area(self) {return self.Side * self.Side;}
}
var sq = Square();
print(sq.Arae);",
@"IDENTIFIER|7:13 Undefined property 'Arae' on <inst Square>.")
                .SetName("Class_Var_Func_Typo_Error");

            yield return new TestCaseData(
@"class Square
{
    var Side = 1;
}
var sq = Square();
print(sq.Side);",
@"1")
                .SetName("ClassVars_InitialValue");

            yield return new TestCaseData(
@"var klass = class Square
{
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
    var a;
    a(){}
}",
@"IDENTIFIER|4:9 Classes cannot have a field and a method of identical names. Found more than 1 a in class Square.")
                .SetName("Class_DupFieldnMethod");

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
@"print(""Hello\r\nWorld!"");",
@"Hello
World!")
                .SetName("Print_EscapedChars");

            yield return new TestCaseData(
@"var a = ""Why "", b = ""Hello "", c = ""There"";
print(a + b + c);",
@"Why Hello There")
                .SetName("MultiVariablePrintMatch");

            yield return new TestCaseData(
@"class TestClass
{
    var a = ""Why "", b = ""Hello "", c = ""There"";
    Say(self) { print(self.a + self.b + self.c); }
}
var tc = TestClass();
tc.Say(tc);",
@"Why Hello There")
                .SetName("MultiVarFieldPrintMatch");

            yield return new TestCaseData(
@"var a = true;
if(a) print(7);
else
{
    print(-7);
}",
@"7")
                .SetName("SingleStatementIf");

            yield return new TestCaseData(
@"var i = 0;
while (i < 10) i += 1;
print(i);",
@"10")
                .SetName("SingleStatementWhile");

            yield return new TestCaseData(
@"class Base { var a = 1; }
class Derived < Base { var b = 2;}
var inst = Derived();
print(inst.b + inst.a);",
@"3")
                .SetName("InherFields");

            yield return new TestCaseData(
@"class Base { BaseMeth(b) {print(b + ""Bar"");} }
class Derived < Base { var c = ""Foo"";}
var inst = Derived();
inst.BaseMeth(inst.c);",
@"FooBar")
                .SetName("Inher_Base_Method");

            yield return new TestCaseData(
@"class Base { BaseMeth(b) {print(b + ""Bar"");} }
class Derived < Base { var c = ""Foo"";}
var inst = Derived();
inst.BaseMeth(inst.c);",
@"FooBar")
                .SetName("InherMethodsAndField");

            yield return new TestCaseData(
@"class Base
{
    var fb = ""Foobar? "";
}
class Derived < Base
{
    class BaseMeth(b) {print(b + ""Bar"");}
    ChildMeth(self,a) {print(""Well, ""); Derived.BaseMeth(a + self.fb);}
}
var inst = Derived();
inst.ChildMeth(inst, ""is it "");",
@"Well, is it Foobar? Bar")
                .SetName("MetaMethodAndInherField");

            yield return new TestCaseData(
@"class Test
{
    var a = 10;
    Thing(self, b)
    {
        if(b == true) { self.a *= 2; return self.a;}
        else { self.a /= 2; return self.a; }
    }
}

var t = Test();
print(t.Thing(t,true));
print(t.Thing(t,false));
",
@"2010")
                .SetName("ClassInnerUseOfThis");

            yield return new TestCaseData(
@"class Test
{
    var a,b,c=10;
    init(self, a,c){}
}

var t = Test(1,2);
printr(t);",
@"<inst Test>
  a : 1
  b : null
  c : 2
  init : <fn init>")
                .SetName("ClassAutoInitVars");

            yield return new TestCaseData(
@"class Test
{
    Meth(self) { self.b += 1; print(self.b); }
}

var t = Test();
t.b = 7;
t.Meth(t);",
@"8")
                .SetName("ResolveAccessOnRuntimeMember");

            yield return new TestCaseData(
@"class Test
{
}

var t = Test();
print(t.a);",
@"IDENTIFIER|6:9 Undefined property 'a' on <inst Test>.")
                .SetName("RuntimeAccessExpection");

            yield return new TestCaseData(
@"var a,b,c=10,d;
a = c;
print(a);",
@"10")
                .SetName("LocalMultiVarDeclare");

            yield return new TestCaseData(
@"fun Meth()
{
    var a = 1, b = 2, z = 3;
    return (a,b,z);
}

var c,d;
(c,d) = Meth();

print(c+d);",
@"3")
                .SetName("MultiReturnsAssigns");

            yield return new TestCaseData(
@"fun Meth()
{
    var a = 1, b = 2, z = 3;
    return (a,b,z);
}

class Obj { var c,d; }

var obj = Obj();

(obj.c,obj.d) = Meth();

print(obj.c + obj.d);",
@"3")
                .SetName("MultiReturnsIntoObjectFields");

            yield return new TestCaseData(
@"fun Meth()
{
    var a = 1, b = 2, z = 3;
    print(b + z);
    return (a);
}

var c,d = 2;
(c) = Meth();

print(c+d);",
@"53")
                .SetName("SingleReturnInGroupingAssigns");

            yield return new TestCaseData(
@"fun Meth()
{
    var a = 1, b = 2, z = 3;
    return (a,b,z);
}

var (c,d) = Meth();

print(c+d);",
@"3")
                .SetName("MultiReturnsVar");

            yield return new TestCaseData(
@"fun Meth()
{
    var a = 1, b = 2, z = 3;
    return (a,b,z);
}

var c,d = Meth();

printr(d);",
@"1")
                .SetName("MultiReturnsTakeFirst");

            yield return new TestCaseData(
@"fun Meth()
{
    var a = 1, b = 2, c = 3;
    return (a,b,c);
}

fun InMeth(i,j,k)
{
    print(i+j+k);
}

InMeth(Meth());",
@"6")
                .SetName("MultiReturnsExpandArgList");

            yield return new TestCaseData(
@"class Vector2
{
    var x,y;
    init(self,x,y){}

    _add(lhs, rhs)
    {
        return Vector2(lhs.x + rhs.x, lhs.y + rhs.y);
    }

    _minus(lhs, rhs)
    {
        return Vector2(lhs.x - rhs.x, lhs.y - rhs.y);
    }

    _slash(lhs, rhs)
    {
        return Vector2(lhs.x / rhs.x, lhs.y / rhs.y);
    }

    _star(lhs, rhs)
    {
        return Vector2(lhs.x * rhs.x, lhs.y * rhs.y);
    }

    _percent(lhs, rhs)
    {
        return Vector2(lhs.x % rhs.x, lhs.y % rhs.y);
    }
}

var a = Vector2(1,2),b = Vector2(3,4);

printr( a+b );
print(""\r\n"");
printr( a-b );
print(""\r\n"");
printr( a*b );
print(""\r\n"");
printr( a/b );
print(""\r\n"");
printr( a%b );",
@"<inst Vector2>
  x : 4
  y : 6
  init : <fn init>
  _add : <fn _add>
  _minus : <fn _minus>
  _slash : <fn _slash>
  _star : <fn _star>
  _percent : <fn _percent>
<inst Vector2>
  x : -2
  y : -2
  init : <fn init>
  _add : <fn _add>
  _minus : <fn _minus>
  _slash : <fn _slash>
  _star : <fn _star>
  _percent : <fn _percent>
<inst Vector2>
  x : 3
  y : 8
  init : <fn init>
  _add : <fn _add>
  _minus : <fn _minus>
  _slash : <fn _slash>
  _star : <fn _star>
  _percent : <fn _percent>
<inst Vector2>
  x : 0.333333333333333
  y : 0.5
  init : <fn init>
  _add : <fn _add>
  _minus : <fn _minus>
  _slash : <fn _slash>
  _star : <fn _star>
  _percent : <fn _percent>
<inst Vector2>
  x : 1
  y : 2
  init : <fn init>
  _add : <fn _add>
  _minus : <fn _minus>
  _slash : <fn _slash>
  _star : <fn _star>
  _percent : <fn _percent>")
                .SetName("ClassMathOperators");

            yield return new TestCaseData(
@"class Vector2
{
    var x,y;
    init(self,x,y){}

    _equality(lhs, rhs)
    {
        return lhs.x == rhs.x and lhs.y == rhs.y;
    }

    _bang_equal(lhs, rhs)
    {
        return lhs.x != rhs.x or lhs.y != rhs.y;
    }
}

var a = Vector2(1,2),b = Vector2(3,4);

print(a == b);

var c = a;

print(a == c);
print(a != b);",
@"FalseTrueTrue")
                .SetName("ClassLogicOperator");

            yield return new TestCaseData(
@"class Scalar
{
    var x;
    init(self,x){}

    _less(lhs, rhs)
    {
        return lhs.x < rhs.x;
    }

    _greater(lhs, rhs)
    {
        return lhs.x > rhs.x;
    }

    _less_equal(lhs, rhs)
    {
        return lhs.x <= rhs.x;
    }

    _greater_equal(lhs, rhs)
    {
        return lhs.x >= rhs.x;
    }
}

var a = Scalar(1), b = Scalar(1);

print(a < b);
print(a > b);
print(a <= b);
print(a >= b);",
@"FalseFalseTrueTrue")
                .SetName("ClassComparisonLogicOperator");

            yield return new TestCaseData(
@"class Vector2
{
    var x,y;
    init(self,x,y){}
}

var a = Vector2(1,2),b = Vector2(3,4);

printr( a+b );",
@"PLUS|9:11 Did not find operator on left instance.")
                .SetName("ClassMissingOperator");

            yield return new TestCaseData(
@"class Vector2
{
    var x,y;
    init(self,x,y){}
}

fun AddV2(lhs, rhs)
{
    return Vector2(lhs.x + rhs.x, lhs.y + rhs.y);
}

var a = Vector2(1,2),b = Vector2(3,4);

printr( AddV2( a, b ) );",
@"<inst Vector2>
  x : 4
  y : 6
  init : <fn init>")
                .SetName("Vector2Func");

            yield return new TestCaseData(
@"throw;",
@"THROW|1:5 ")
                .SetName("ThrowEmptyCaught");

            yield return new TestCaseData(
@"throw ""hello"";",
@"THROW|1:5 hello")
                .SetName("ThrowMsgCaught");

            yield return new TestCaseData(
@"test {var a = 10; print(a);}",
@"10")
                .SetName("AnonTest");

            yield return new TestCaseData(
@"test TestA {print(testName); }",
@"TestA")
                .SetName("NamedTest");

            yield return new TestCaseData(
@"testcase Test1 { print(""Hello World""); }",
@"Hello World")
                .SetName("TestCaseRun");

            yield return new TestCaseData(
@"testcase Test1 (""Foo"",""Bar"") { print(""Hello "" + testValue); }",
@"Hello FooHello Bar")
                .SetName("TestCaseValuesRun");

            yield return new TestCaseData(
@"test TestA 
{
    testcase Test1 {print(testName + "":"" + testCaseName);} 
}",
@"TestA:Test1")
                .SetName("NamedTestCase");

            yield return new TestCaseData(
@"test mathTests
{
    testcase add 
    {
        var a = 1 + 2;
        if(a != 3)
            throw;
    }
    testcase mul 
    {
        var a = 1 * 2;
        if(a != 2)
            throw;
    }
    testcase div 
    {
        var a = 1 / 2;
        if(a != 0.5)
            throw;
    }
    testcase sub 
    {
        var a = 1 - 2;
        if(a != -1)
            throw;
    }
    print(""no failures"");
}",
@"no failures")
                .SetName("SimpleTestSuite");

            yield return new TestCaseData(
@"test mathTests
{
    testcase add 
    {
        var a = 1 + 2;
        if(a != 3)
            throw;
    }
    testcase mul 
    {
        var a = 1 * 2;
        if(a != 1)
            throw;
    }
}",
@"mathTests FAILED 1 of 2
mul:Failed - THROW|13:29 
")
                .SetName("SimpleTestSuiteFailure");

            yield return new TestCaseData(
@"testcase mul 
{
    var a = 1 * 2;
    if(a != 1)
        throw;
}",
@"THROW|5:21 ")
                .SetName("TestCaseFailure");

            yield return new TestCaseData(
@"class MyClass
{
    var a,b,c = 10;
    init(self, a,b)
    {
        self.c += a + b;
    }
}

var my = MyClass(1,2);
print(my.c);",
@"13")
                .SetName("Class_Create_init");

            yield return new TestCaseData(
@"fun Func()
{
    print(""Foo"");
    return;
    print(""Bar"");
}

Func();",
@"Foo")
                .SetName("Return_void");

            yield return new TestCaseData(
@"print("""");",
@"")
                .SetName("Empty");
        }

        [Test]
        [TestCaseSource(nameof(Generator))]
        public void Interpreter_StringifiedResult_Matches(string testString, string requiredResult)
        {
            var engine = new InterpreterTestLoxEngine();

            engine.Run(testString, true);

            Assert.AreEqual(requiredResult, engine.InterpreterResult);
        }

        internal class InterpreterTestLoxEngine : TestLoxEngine
        {
            public InterpreterTestLoxEngine()
                : base()
            {
            }
        }
    }
}
