# ulox-work
Worked through [Crafting Interpreters](http://craftinginterpreters.com/), in Unity C#. Now I'm just noodling around.

## Why though?
Don't expect this to be used (very much) in production. At least not in the state it is right now. Honestly it was something fun to work through, a pleasent sparetime distraction.

Beyond a port of jlox, it is now something to toy with, optimize, prototype with, mess with features and usability. 

## Status 
ulox is no longer a superset of lox. The last point where that was the case is at [this commit](../../tree/core_jlox_varloc)

![current code coverage](badge_linecoverage.png)
- [Cache Gets](../../tree/ulox_assign_variable_collapse)
	- Cache slot indicies on get and set expressions. Greatly improves performance of methods that operate on instance fields.
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
	- Add support for chainning declarations, see [Class Sugar](#class-sugar).
	- Each Environemnt now a flat list of objects with a string to index lookup. Used and stored in Exprs (VarLocs).
		- Parser marks the VarLoc as 'Invalid'.
		- Resolver attempts to determine the VarLoc, being ancesor and slot of identifiers.
		- Interpreter will 'find' identifiers by Lexeme if VarLoc is invalid and save the VarLoc for next time.
	- Resolver tracks use of variables in each scope, for future use.
	- Class fields are made base first, as expected in other languagues. This ensures VarLoc order is the same for super methods in child classes.
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
		- Instance is now an Environment, allowing external code to ask for objects by heirarchy path.
- [Core jlox Challenges and sugar](../../tree/core_jlox_chall) Core jlox equivilant plus many of the challenges from the book and additional sugar.
	-  Conditional operator.
	-  More error reporting.
	-  Many more tests.
	-  String coercion in operators.
	-  Keywords 'loop'(while(true)), 'break', and 'continue'.
	-  Lambdas.
	-  Resolver Warnings for unused variables.
	-  Class call-less properties, they're methods that do not declare or store a param list.
	-  MetaClasses and MetaMethods, using class keyword inside class declare is akin to static.
	-  Added operator %, +=, -=, \*=, /=, %=.
	-  Add Array type to engine.
	-  Add sugar to class declarations: 'get', 'set', 'getset', 'var' keywords. [Class Sugar](#class-sugar)
- [Core jlox](../../tree/core_jlox) Core jlox equivilant implemented in C# inside a Unity project using the TestRunner and Code Coverage to confirm correct behavior and error reporting.

## Pending
- Add Unity specific hooks and functions.
- Add Performance tests.
- Add/port tests from (craftinginterpreters)[https://github.com/munificent/craftinginterpreters/tree/master/test]
- Add Testing and Asserting library to lox.
- Multiple return values.
- Implicit this within methods.

## Differences
- null instead of nil.
- [Class Sugar](#class-sugar).
- An Instance is an Environment.
- print is no longer a statement. It's now a function in the LoxCoreLibrary. 
- Globals are in an instance, rather than an environment and bind themselves with the name 'Globals'. Allows for adding to globals from lower deeper scopes.

## Class Sugar
The added keywords in class declarations minimise the amount of plumbing required to do basic things.

```
class Box
{
	init() 
	{
		this.width = 1; 
		this.height = 1; 
	}
	area { return this.width * this.height; }
}
```
Can be shorted with 'var'. If initial values are not required a simple ``` var a;``` will initialise it to null.
```
class Square
{
	var width = 1;
	var heigth = 1;
	area { return this.width * this.height; }
}
```
The 'get', 'set', and 'getset' assist with C# style auto properties. Here we'll show what the resulting class looks like internally.
```
class Foo
{
	get a;
	set b = 1;
	getset c = "Hello";
}
```
Becomes akin to.
```
class Foo
{
	init() 
	{
		this._a = null;
		this._b = 1;
		this._c = "Hello";
	}
	
	a { return this._a; }
	
	Setb(value) { this._b = value; }
	
	c { return this._c; }
	Setc(value) { this._c = value; }
}
```
This is done by generating the AST nodes in the parser. 

Note: the backing fields, are always named \_VAR_NAME (prepended with an underscore). These backing fields are set up by the interperter before the class init is called, this allows the backing fields to be used directly in the init function itself.

You can chain together multiple variable declares in 1 statement.
```
class Foo
{
	var a, b = 2, c = "hello";
	getset d, e = 7, d;
}
```
is expanded by the parser to be equivilant in interpretation as 
```
class Foo
{
	var a;
	var b = 2;
	var c = "hello";
	getset d;
	getset e = 7;
	getset d;
}
```

Class init arguments that match name of a class field and located in resolver and automatically assigned by index during class instance creation.
Allows for the removal of ```this.x = x;``` that would clutter an init method when default values are not know to the class.
```
class Foo
{
	var a, b, c;
	
	init(a,b,c)
	{
		this.a = a;
		this.b = b;
		this.c = c;
	}
}
```
Can be simplified to
```
class Foo
{
	var a, b, c;
	
	init(a,b,c)
	{
	}
}
```
and the field assignment from params will happen automatically.


