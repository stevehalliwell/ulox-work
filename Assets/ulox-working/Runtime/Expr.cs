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
            public Get( Expr obj, Token name)
            {
                this.obj = obj;
                this.name = name;
            }
            public readonly Expr obj;
            public readonly Token name;
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
        public class Set : Expr
        {
            public Set( Expr obj, Token name, Expr val)
            {
                this.obj = obj;
                this.name = name;
                this.val = val;
            }
            public readonly Expr obj;
            public readonly Token name;
            public readonly Expr val;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class Super : Expr
        {
            public Super( Token keyword, Token method)
            {
                this.keyword = keyword;
                this.method = method;
            }
            public readonly Token keyword;
            public readonly Token method;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }
        public class This : Expr
        {
            public This( Token keyword)
            {
                this.keyword = keyword;
            }
            public readonly Token keyword;
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
        public class Variable : Expr
        {
            public Variable( Token name)
            {
                this.name = name;
            }
            public readonly Token name;
            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);
        }

        public abstract T Accept<T>(Visitor<T> visitor);

        public interface Visitor<T> 
        {
            T Visit(Assign expr);
            T Visit(Binary expr);
            T Visit(Call expr);
            T Visit(Get expr);
            T Visit(Grouping expr);
            T Visit(Literal expr);
            T Visit(Logical expr);
            T Visit(Set expr);
            T Visit(Super expr);
            T Visit(This expr);
            T Visit(Unary expr);
            T Visit(Variable expr);
        }
    }
}
