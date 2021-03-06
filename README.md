# ulox-work

No further changes expected here. Future experiements, noodling, and messing around will be structed and take place over here, [ulox](https://github.com/stevehalliwell/ulox).

Worked through [Crafting Interpreters](http://craftinginterpreters.com/), in Unity C#. Both Tree-Walking and ByteCode interpreters.

## Why though?

Don't expect this to be used (very much) in production. At least not in the state it is right now. Honestly it was something fun to work through, a pleasant sparetime distraction.

Beyond a port of jlox and clox, it was something to toy with, optimize, prototype with, mess with features and usability.

## Status

### ByteCode

ByteCode style interpreter, akin to clox, has parity with some minor changes and a few additions. Details below.

<details>
<summary>Show Details</summary>
Less variants and experiements exist on the bytecode version that on the interpreter, none the less, it can run a bouncing balls demo and does so faster and with FAR less garbage being created. In simple release comparison to native C# unity demo is it ~0.07ms per frame, and the ByteCode interpreter is ~0.33ms, a very pleaseant only ~5x slowdown.

- [Challenges, Sugar, and Demos](../../tree/bytecode_vars)
  - RLE line numbers.
  - Add loop, break, and continue keywords.
  - Remove print instruction, use standard function instead.
  - Replace default stacks and lists with faster, lazier version. Improves perf related to push and pop of value stack and callframes.
  - Cache current callframe.
  - Add Throw OpCode.
  - Add Cached and Uncached global OpCodes, allows the VM to rewrite the OpCode to a cached version. Improves performance of all subsequent calls.
  - Add OpCode for common constants and bytecode stream encoded small integers.
  - Add class var and default values for class vars, see [ByteCode Class Sugar](#bytecode-class-sugar). Adds OpCodes for Property and Init_Chain_Start.

- [Core clox](../../tree/core_clox) Core clox equivalent implemented in C# inside a Unity project using the TestRunner and Bouncing to confirm correct behavior and error reporting.

#### Differences

-----

- null instead of nil.
- Reusing scanner from jlox port, rather than token at a tme.
- No manual memory management, host language does this for us.

#### ByteCode Class Sugar

-----

We can add fields to a class with the `var` keyword, this is functionally the same as declaring them in the class `init` but less boilerplate.

```cs
class T
{
  init()
  {
    this.a = null;
    this.b = null;
    this.c = null;
  }
}
```

Can be simplified to

```cs
class T
{
  var a,b,c;
}
```

These var style declares can be chained as above and can have default values given. This is acheived by having the compiler weave jumps, get locals, expression, set properties, segments within the class property code. These chains are linked by the compiler and set via an OpCode in the class. When an instance is created, the chain is executed like a function before the init, if one exists, is run.

Allowing for things like

```cs
var SomeGlobal = 2;

class T
{
  var a = 7,b = SomeGlobal,c;
}
```

</details>

### Tree-Walk

Tree-Walk style interpreter, akin to jlox, has had a lot of variants and experiments done to it, you can explore it in stages if you wish to via the repo tags.

<details>
<summary>Show Details</summary>
ulox is no longer a superset of jlox. The last point where that was the case is at [this commit](../../tree/core_jlox_varloc)

- [Closureless Variant](../../tree/closureless)
  - This adds the explicit concept of an environment stack to the interpreter.
  - Removes explicit closures being made around everything. The only test that fails is MakeCounter.  

- [Methodless Variant](../../tree/methodless)

  - This removes methods from classes in their entirety. No more `this`, not more `super`, all functions declared inside classes belong to the class not the instance.
  - Removed unsafe type assuming optimizations, such as environment indices.
  - Remove many tests related to methods.
  - Add more tests.

- [Testing library and Asserts](../../tree/ulox_testing_library)
  
  - Add Testing statements and library to ulox see [Testing](#testing).
  - Add Assert.AreEqual and Assert.AreApproxEqual.
  - Allow classes to implement their own binary [operator overloads](#operator-overloading) .

- [Implicit This, Multiple Returns, classof](../../tree/ulox_this_multi_return_classof)
  
  - The use of member vars within a method, no longer require explicit `this.` The this is inserted automatically if the variable is not in local or enclosing scope but is found in a valid local this.
  - Add 'classof' engine function, like a c# typeof, it locates the class of the passed instance. This can also also be used to make new instances `var newObj = classof(existingObject)();`.
  - Support multiple return values and splicing assignments/initialisers.See [Multiple Return](#multiple-return-values).

- [Cache Gets](../../tree/ulox_assign_variable_collapse)
  
  - Cache slot indices on get and set expressions. Greatly improves performance of methods that operate on instance fields.
  - Class init params that match field names are automatically assigned before init body is run, see [Class Sugar](#class-sugar).
  - Add bouncing ball variants, using ulox and monobehaviours in different ways to give performance comparisons.
  - Collapse Expr.Assign into Expr.Set and Expr.Variable into Expr.Get.

- [Libraries](../../tree/ulox_libraries)
  
  - Refactor hardwired parts of LoxEngine into modules/libraries.
  - print and printr functions added. Removed print statement.
  - Add REPL demo.
  - Add ScriptPerObject Unity Demo.
  - Sandboxing, running scripts in a local environment with no access to it's enclosing.
  - Additional tests.

- [Use of VarLoc, more QOL, more Tests](../../tree/core_jlox_varloc)
  
  - Add support for chaining declarations, see [Class Sugar](#class-sugar).
  - Each Environment now a flat list of objects with a string to index lookup. Used and stored in Exprs (VarLocs).
    - Parser marks the VarLoc as 'Invalid'.
    - Resolver attempts to determine the VarLoc, being ancestor and slot of identifiers.
    - Interpreter will 'find' identifiers by Lexeme if VarLoc is invalid and save the VarLoc for next time.
  - Resolver tracks use of variables in each scope, for future use.
  - Class fields are made base first, as expected in other languages. This ensures VarLoc order is the same for super methods in child classes.
  - Super supports optional token in bracket to specify the name of the base class being targeted by the super.
  - More Tests.

- [Unity demos and some QOL improvements](../../tree/core_jlox_unity_demos)
  
  - More tests.
  - Bouncing Balls
    - Unity runs script start and update.
    - Script bounces objects around.
  - Breakout style game
    - Unity runs script start, update, and collisions.
    - Script creates objects, moves paddle, and handles game mode and lives.
  - Add List and POD types to engine.
  - Add QOL functions to engine, allowing embedding application to be less aware of the inner workings.
  - Fixes and changes:
    - Allow multiple requests to interpreter resolve with the same data.
    - Line and Character numbering corrected
    - Instance is now an Environment, allowing external code to ask for objects by hierarchy path.

- [Core jlox Challenges and sugar](../../tree/core_jlox_chall) Core jlox equivalent plus many of the challenges from the book and additional sugar.
  
  - Conditional operator.
  - More error reporting.
  - Many more tests.
  - String coercion in operators.
  - Keywords 'loop'(while(true)), 'break', and 'continue'.
  - Lambdas.
  - Resolver Warnings for unused variables.
  - Class call-less properties, they're methods that do not declare or store a param list.
  - MetaClasses and MetaMethods, using class keyword inside class declare is akin to static.
  - Added operator %, +=, -=, \*=, /=, %=.
  - Add Array type to engine.
  - Add sugar to class declarations: 'get', 'set', 'getset', 'var' keywords. [Class Sugar](#class-sugar)

- [Core jlox](../../tree/core_jlox) Core jlox equivalent implemented in C# inside a Unity project using the TestRunner and Code Coverage to confirm correct behavior and error reporting.

#### Differences

-----

- null instead of nil.
- [Class Sugar](#class-sugar).
- An Instance is an Environment.
- print is no longer a statement. It's now a function in the LoxCoreLibrary.
- Globals are in an instance, rather than an environment and bind themselves with the name 'Globals'. Allows for adding to globals from lower deeper scopes.

#### Operator Overloading

-----

Classes can implement their own operators for binary operators, specifically the following; `_bang_equal, _equality, _greater, _greater_equal, _less, _less_equal, _minus, _add, _slash, _star, _percent` as a method within it's class, like the following.

```cs
class Vector2
{
    var x,y;
    init(x,y){}

    _add(lhs, rhs)
    {
        return Vector2(lhs.x + rhs.x, lhs.y + rhs.y);
    }
}
```

#### Multiple Return Values

-----

Functions can return more than 1 value by enclosing multiple expressions in braces. `return (a,b,c);`

These returns are expanded and sliced in a number of useful ways.

If as arguments to a call, they are expanded order-wise.

```cs
fun Meth()
{
    var a = 1, b = 2, c = 3;
    return (a,b,c);
}

fun InMeth(i,j,k)
{
    print(i+j+k);
}

InMeth(Meth());
```

Assigning results to multiple variables is done order-wise. This is done by eclosing the identifiers in `()`. Any excess are dropped, if not enough are returned it initial values are unchanged.

```cs
var a,b,c;
(a,b,c) = Meth(); a is 1, b is 2, c is 3

var (d,e,f) = Meth(); d is 1, e is 2, f is 3
```

If regular assign or var receives a multiple return values, it simply takes the first and ignores the rest.

```cs
var x,y = Meth();// x is null, y is 1
```

#### Class Sugar

-----

The variant you are looking at right now has reduced class functionality as it is methodless. This puts us somewhere between fully duck typed c and python.

Can be shorted with 'var'. If initial values are not required a simple ```var a;``` will initialise it to null.

```cs
class Square
{
    var width = 1;
    var heigth = 1;
    
    Area(self) { return self.width * self.height; }
}

var sq = new Square(2,3);
var area = Square.Area(sq);
```

You can chain together multiple variable declares in 1 statement.

```cs
class Foo
{
    var a, b = 2, c = "hello";
}
```

is expanded by the parser to be equivilant in interpretation as;

```cs
class Foo
{
    var a;
    var b = 2;
    var c = "hello";
}
```

Class instances have their init method called with themselves as the first param, `self`.

```cs
class T
{
    init(self)
    {
        self.a = "Foo";
    }
}

var t = T();    //t.a will be Foo
```

Class init arguments that match name of a class field and located automatically assigned during class instance creation.
Allows for the removal of ```self.x = x;``` that would clutter an init method when default values are not know to the class.

```cs
class Foo
{
    var a, b, c;

    init(self,a,b,c)
    {
        self.a = a;
        self.b = b;
        self.c = c;
    }
}
```

Can be simplified to

```cs
class Foo
{
    var a, b, c;

    init(a,b,c) { }
}
```

and the field assignment from params will happen automatically.

In both cases this is used as follows.

```cs
var f = Foo(1,2,3);
```

#### Testing

------

Testing statements are built into the language itself now.

A `test` statement, has an optional name and then contains a block. If testing is turned off within the engine nothing within the test block is emitted by the parser. This allows writing functions, vars, classes and so on that only exist if testing is enabled.

A `testcase` statement, must have a named, and optional grouping (more on this later) and a block.  TestCases can be declared under a test or outside of one, in which case they are automatically assigned to the anonymous test suite.  

With testing enabled all test suites will be run, to fail a test use the Assert library or the throw statement.

The simplest test case, will auto run in an anonymous test suite and since it cannot throw, will pass.

```cs
testcase Test1 { }
```

 Similarly a test case that will always fail

```cs
testcase Test2 { throw; }

//or perhaps

testcase Test2 { throw "Not Implemented"; }
```

If you wish to group and/or be able to turn off test cases, then they should be inside a test statement.

```cs
test MyTestSuite
{
    testcase Test3 { }
}
```

The optional grouping on test cases, results in the testcase be called once per grouping expression, with the result of the expression assigned to the variable `testValue` inside the block.

``` cs
testcase Test4 (""Foo"",""Bar"") 
{ 
    print(""Hello "" + testValue); 
}
//prints 
```

A more complete example.

```cs
test IntegerMath
{
    var a = 4, b = 2;
    var AddResult = 6;
    var MulResult = 8;
    var SubResult = 2;
    var DivResult = 2;
    var ModResult = 0;

    testcase Add
    {
        Assert.AreEqual(a+b, AddResult, testCaseName);
    }

    testcase Mul
    {
        Assert.AreEqual(a*b, MulResult, testCaseName);
    }

    testcase Sub
    {
        Assert.AreEqual(a-b, SubResult, testCaseName);
    }

    testcase Div
    {
        Assert.AreEqual(a/b, DivResult, testCaseName);
    }
}
```

Inside a testcase there is a variable `testCaseName` which olds the name of the testcase.  Within a test, there is a variable `testName`.


</details>
