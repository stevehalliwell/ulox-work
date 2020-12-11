using System.Collections.Generic;

namespace ULox
{
    public abstract class Stmt
    {
        public class Block : Stmt
        {
            public Block( List<Stmt> statements)
            {
                this.statements = statements;
            }
            public readonly List<Stmt> statements;
            public override void Accept(Visitor visitor) => visitor.Visit(this);
        }
        public class Expression : Stmt
        {
            public Expression( Expr expression)
            {
                this.expression = expression;
            }
            public readonly Expr expression;
            public override void Accept(Visitor visitor) => visitor.Visit(this);
        }
        public class Print : Stmt
        {
            public Print( Expr expression)
            {
                this.expression = expression;
            }
            public readonly Expr expression;
            public override void Accept(Visitor visitor) => visitor.Visit(this);
        }
        public class Var : Stmt
        {
            public Var( Token name, Expr initializer)
            {
                this.name = name;
                this.initializer = initializer;
            }
            public readonly Token name;
            public readonly Expr initializer;
            public override void Accept(Visitor visitor) => visitor.Visit(this);
        }

        public abstract void Accept(Visitor visitor);

        public interface Visitor 
        {
            void Visit(Block stmt);
            void Visit(Expression stmt);
            void Visit(Print stmt);
            void Visit(Var stmt);
        }
    }
}
