using System.Collections.Generic;

namespace ULox
{
    public abstract class Expr
    {
        public class Set : Expr
        {
            public Set( Expr obj, Token name, Expr val, EnvironmentVariableLocation varLoc)
            {
                this.obj = obj;
                this.name = name;
                this.val = val;
                this.varLoc = varLoc;
            }
            public readonly Expr obj;
            public readonly Token name;
            public readonly Expr val;
            public EnvironmentVariableLocation varLoc;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Binary : Expr
        {
            public Binary( Expr left, Token op, Expr right)
            {
                this.left = left;
                this.op = op;
                this.right = right;
            }
            public readonly Expr left;
            public readonly Token op;
            public readonly Expr right;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Call : Expr
        {
            public Call( Expr callee, Token paren, List<Expr> arguments)
            {
                this.callee = callee;
                this.paren = paren;
                this.arguments = arguments;
            }
            public readonly Expr callee;
            public readonly Token paren;
            public readonly List<Expr> arguments;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Get : Expr
        {
            public Get( Expr obj, Token name, short knownSlot)
            {
                this.obj = obj;
                this.name = name;
                this.knownSlot = knownSlot;
            }
            public readonly Expr obj;
            public readonly Token name;
            public short knownSlot;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Variable : Expr
        {
            public Variable( Token name, EnvironmentVariableLocation varLoc)
            {
                this.name = name;
                this.varLoc = varLoc;
            }
            public readonly Token name;
            public EnvironmentVariableLocation varLoc;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Grouping : Expr
        {
            public Grouping( Expr expression)
            {
                this.expression = expression;
            }
            public readonly Expr expression;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Literal : Expr
        {
            public Literal( object value)
            {
                this.value = value;
            }
            public readonly object value;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Logical : Expr
        {
            public Logical( Expr left, Token op, Expr right)
            {
                this.left = left;
                this.op = op;
                this.right = right;
            }
            public readonly Expr left;
            public readonly Token op;
            public readonly Expr right;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Super : Expr
        {
            public Super( Token keyword, Token classNameToken, Token method,  EnvironmentVariableLocation superVarLoc, EnvironmentVariableLocation thisVarLoc)
            {
                this.keyword = keyword;
                this.classNameToken = classNameToken;
                this.method = method;
                this.superVarLoc = superVarLoc;
                this.thisVarLoc = thisVarLoc;
            }
            public readonly Token keyword;
            public readonly Token classNameToken;
            public readonly Token method;
            public EnvironmentVariableLocation superVarLoc;
            public EnvironmentVariableLocation thisVarLoc;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class This : Expr
        {
            public This( Token keyword, EnvironmentVariableLocation varLoc)
            {
                this.keyword = keyword;
                this.varLoc = varLoc;
            }
            public readonly Token keyword;
            public EnvironmentVariableLocation varLoc;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Unary : Expr
        {
            public Unary( Token op, Expr right)
            {
                this.op = op;
                this.right = right;
            }
            public readonly Token op;
            public readonly Expr right;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Conditional : Expr
        {
            public Conditional( Expr condition, Expr ifTrue, Expr ifFalse)
            {
                this.condition = condition;
                this.ifTrue = ifTrue;
                this.ifFalse = ifFalse;
            }
            public readonly Expr condition;
            public readonly Expr ifTrue;
            public readonly Expr ifFalse;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Function : Expr
        {
            public Function( List<Token> parameters, List<Stmt> body, bool HasLocals, bool NeedsClosure,  bool HasReturns)
            {
                this.parameters = parameters;
                this.body = body;
                this.HasLocals = HasLocals;
                this.NeedsClosure = NeedsClosure;
                this.HasReturns = HasReturns;
            }
            public readonly List<Token> parameters;
            public readonly List<Stmt> body;
            public bool HasLocals;
            public bool NeedsClosure;
            public bool HasReturns;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }

        public abstract T Accept<T>(Visitor<T> visitor);

        public interface Visitor<T>
        {
            T Visit(Set expr);
            T Visit(Binary expr);
            T Visit(Call expr);
            T Visit(Get expr);
            T Visit(Variable expr);
            T Visit(Grouping expr);
            T Visit(Literal expr);
            T Visit(Logical expr);
            T Visit(Super expr);
            T Visit(This expr);
            T Visit(Unary expr);
            T Visit(Conditional expr);
            T Visit(Function expr);
        }
    }
}
