using System.Text;

namespace ULox
{
    public class ASTPrinter : Expr.Visitor<string>
    {
        public string Print(Expr expr) => expr.Accept(this);

        public string Visit(Expr.Binary expr) => Parenthesize(expr.op.Lexeme, expr.left, expr.right);

        public string Visit(Expr.Grouping expr) => Parenthesize("group", expr.expression);

        public string Visit(Expr.Literal expr) => expr.value?.ToString() ?? "null";

        public string Visit(Expr.Unary expr) => Parenthesize(expr.op.Lexeme, expr.right);

        public string Visit(Expr.Variable expr)
        {
            throw new System.NotImplementedException();
        }

        public string Visit(Expr.Assign expr)
        {
            throw new System.NotImplementedException();
        }

        public string Visit(Expr.Logical expr)
        {
            throw new System.NotImplementedException();
        }

        public string Visit(Expr.Call expr)
        {
            throw new System.NotImplementedException();
        }

        private string Parenthesize(string name, params Expr[] list)
        {
            var sb = new StringBuilder();
            sb.Append("(");
            sb.Append(name);
            foreach (var expr in list)
            {
                sb.Append(" ");
                sb.Append(expr.Accept(this));
            }
            sb.Append(")");

            return sb.ToString();
        }
    }
}
