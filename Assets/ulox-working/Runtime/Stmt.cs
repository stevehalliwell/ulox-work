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
        public class Function : Stmt
        {
            public Function( Token name, List<Token> parameters, List<Stmt> body)
            {
                this.name = name;
                this.parameters = parameters;
                this.body = body;
            }
            public readonly Token name;
            public readonly List<Token> parameters;
            public readonly List<Stmt> body;
            public override void Accept(Visitor visitor) => visitor.Visit(this);
        }
        public class If : Stmt
        {
            public If( Expr condition, Stmt thenBranch, Stmt elseBranch)
            {
                this.condition = condition;
                this.thenBranch = thenBranch;
                this.elseBranch = elseBranch;
            }
            public readonly Expr condition;
            public readonly Stmt thenBranch;
            public readonly Stmt elseBranch;
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
        public class Return : Stmt
        {
            public Return( Token keyword, Expr value)
            {
                this.keyword = keyword;
                this.value = value;
            }
            public readonly Token keyword;
            public readonly Expr value;
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
        public class While : Stmt
        {
            public While( Expr condition, Stmt body)
            {
                this.condition = condition;
                this.body = body;
            }
            public readonly Expr condition;
            public readonly Stmt body;
            public override void Accept(Visitor visitor) => visitor.Visit(this);
        }

        public abstract void Accept(Visitor visitor);

        public interface Visitor 
        {
            void Visit(Block stmt);
            void Visit(Expression stmt);
            void Visit(Function stmt);
            void Visit(If stmt);
            void Visit(Print stmt);
            void Visit(Return stmt);
            void Visit(Var stmt);
            void Visit(While stmt);
        }
    }
}
