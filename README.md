# ulox-work
Working through [Crafting Interpreters](http://craftinginterpreters.com/), in Unity C#.

## Why though?
Don't expect this to be used (very much) in production. At least not in the state it is right now. Honestly it was something fun to work through, a pleasent sparetime distraction.

## Status 
![current code coverage](badge_linecoverage.png)
- [insert tag] Core jlox equivilant plus many of the challanges from the book and additional sugar.
	-  Conditional operator.
	-  More error reporting.
	-  Many more tests.
	-  String coercion in operators.
	-  Keywords 'loop'(while(true)), 'break', and 'continue'.
	-  Lambdas.
	-  Resolver Warnings for unused variables.
	-  Class call-less properties, they're class methods that do not declare or store any param list.
	-  MetaClasses and MetaMethods, using class keyword inside class declare is akin to static.
	-  Added operator %, +=, -=, \*=, /=, %=.
	-  Add Array type to engine.
	-  Add sugar to class declarations: 'get','set','getset','var' keywords.[Class Sugar](#class-sugar)
- [insert tag] Core jlox equivilant implemented in C# inside a Unity project using the TestRunner and Code Coverage to confirm correct behavior and error reporting.

## Pending
- Add standard lib equivilant for interpreter.
- Add Unity specific hooks and functions.
- Add REPL support.
- Add Performance tests.
- Increment and Decrement (maybe).

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
	area { return this.side * this.side; }
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
Note: the backing fields, are always named \_VAR_NAME and setup by the interperter before the classes init is called, so they can be used directly in the init function itself.
