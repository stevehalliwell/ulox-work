# ulox-work
Working through [Crafting Interpreters](http://craftinginterpreters.com/), in Unity C#.

## Why though?
Don't expect this to be used (very much) in production. At least not in the state it is right now. Honestly it was something fun to work through, a pleasent sparetime distraction.

## Status 
![current code coverage](badge_linecoverage.png)
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
- Add standard lib equivilant for interpreter.
- Add Unity specific hooks and functions.
- Add REPL support.
- Add Performance tests.
- Increment and Decrement (maybe).

## Differences
- null instead of nil.
- [Class Sugar](#class-sugar).
- An Instance is an Environment.

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
