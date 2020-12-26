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
        public class Class : Stmt
        {
            public Class( Token name, Expr.Variable superclass, List<Stmt.Function> methods, List<Stmt.Function> metaMethods, List<Token> fields, List<Token> metaFields)
            {
                this.name = name;
                this.superclass = superclass;
                this.methods = methods;
                this.metaMethods = metaMethods;
                this.fields = fields;
                this.metaFields = metaFields;
            }
            public readonly Token name;
            public readonly Expr.Variable superclass;
            public readonly List<Stmt.Function> methods;
            public readonly List<Stmt.Function> metaMethods;
            public readonly List<Token> fields;
            public readonly List<Token> metaFields;
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
            public Function( Token name, Expr.Function function)
            {
                this.name = name;
                this.function = function;
            }
            public readonly Token name;
            public readonly Expr.Function function;
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
            public While( Expr condition, Stmt body, Stmt increment)
            {
                this.condition = condition;
                this.body = body;
                this.increment = increment;
            }
            public readonly Expr condition;
            public readonly Stmt body;
            public readonly Stmt increment;
            public override void Accept(Visitor visitor) => visitor.Visit(this);
        }
        public class Break : Stmt
        {
            public Break( Token keyword)
            {
                this.keyword = keyword;
            }
            public readonly Token keyword;
            public override void Accept(Visitor visitor) => visitor.Visit(this);
        }
        public class Continue : Stmt
        {
            public Continue( Token keyword)
            {
                this.keyword = keyword;
            }
            public readonly Token keyword;
            public override void Accept(Visitor visitor) => visitor.Visit(this);
        }

        public abstract void Accept(Visitor visitor);

        public interface Visitor 
        {
            void Visit(Block stmt);
            void Visit(Class stmt);
            void Visit(Expression stmt);
            void Visit(Function stmt);
            void Visit(If stmt);
            void Visit(Print stmt);
            void Visit(Return stmt);
            void Visit(Var stmt);
            void Visit(While stmt);
            void Visit(Break stmt);
            void Visit(Continue stmt);
        }
    }
}
