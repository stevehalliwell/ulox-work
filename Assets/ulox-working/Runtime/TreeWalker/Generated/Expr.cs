using System.Collections.Generic;

namespace ULox
{
    public abstract class Expr
    {
        public class Assign : Expr
        {
            public Assign( Token name, Expr value)
            {
                this.name = name;
                this.value = value;
            }
            public readonly Token name;
            public readonly Expr value;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Set : Expr
        {
            public Set( Expr targetObj, Token name, Expr val)
            {
                this.targetObj = targetObj;
                this.name = name;
                this.val = val;
            }
            public Expr targetObj;
            public readonly Token name;
            public readonly Expr val;
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
            public Call( Expr callee, Token paren, Expr.Grouping arguments)
            {
                this.callee = callee;
                this.paren = paren;
                this.arguments = arguments;
            }
            public readonly Expr callee;
            public readonly Token paren;
            public readonly Expr.Grouping arguments;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Get : Expr
        {
            public Get( Expr targetObj, Token name)
            {
                this.targetObj = targetObj;
                this.name = name;
            }
            public Expr targetObj;
            public readonly Token name;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Grouping : Expr
        {
            public Grouping( List<Expr> expressions)
            {
                this.expressions = expressions;
            }
            public readonly List<Expr> expressions;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Variable : Expr
        {
            public Variable( Token name)
            {
                this.name = name;
            }
            public readonly Token name;
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
            public Function( List<Token> parameters, List<Stmt> body)
            {
                this.parameters = parameters;
                this.body = body;
            }
            public readonly List<Token> parameters;
            public readonly List<Stmt> body;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Throw : Expr
        {
            public Throw( Token keyword, Expr expr)
            {
                this.keyword = keyword;
                this.expr = expr;
            }
            public readonly Token keyword;
            public readonly Expr expr;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }

        public abstract T Accept<T>(Visitor<T> visitor);

        public interface Visitor<T>
        {
            T Visit(Assign expr);
            T Visit(Set expr);
            T Visit(Binary expr);
            T Visit(Call expr);
            T Visit(Get expr);
            T Visit(Grouping expr);
            T Visit(Variable expr);
            T Visit(Literal expr);
            T Visit(Logical expr);
            T Visit(Unary expr);
            T Visit(Conditional expr);
            T Visit(Function expr);
            T Visit(Throw expr);
        }
    }
}
