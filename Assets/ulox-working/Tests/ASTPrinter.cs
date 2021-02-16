using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ULox.Tests
{
    public class ASTPrinter : Expr.Visitor<Object>,
                            Stmt.Visitor
    {
        private StringBuilder stringBuilder = new StringBuilder();
        private int indent = 0;

        private void Indent()
        {
            indent++;
        }

        private void Dent()
        {
            indent--;
        }

        private void PrintLine()
        {
            stringBuilder.AppendLine();
            var spacer = string.Concat(Enumerable.Repeat("  ", indent));
            stringBuilder.Append(spacer);
        }

        public string FinalString
        {
            get
            {
                var resultingString = stringBuilder.ToString();
                System.IO.File.WriteAllText("parser_test_out.txt", resultingString);
                return resultingString;
            }
        }

        public void Print(List<Stmt> stmts)
        {
            for (int i = 0; i < stmts.Count; i++)
            {
                var item = stmts[i];
                Print(item);

                if (i < stmts.Count - 1)
                    PrintLine();
            }
        }

        public void Print(Stmt stmt)
        {
            if (stmt == null) return;
            stringBuilder.Append("{ ");
            stmt.Accept(this);
            stringBuilder.Append(" }");
        }

        public void Print(List<Expr> exprs)
        {
            foreach (var item in exprs)
                Print(item);
        }

        public void Print(Expr expr)
        {
            if (expr == null) return;
            stringBuilder.Append("[ ");
            expr.Accept(this);
            stringBuilder.Append(" ]");
        }

        public void Print(List<Token> tokens)
        {
            stringBuilder.Append(" ( ");
            for (int i = 0; i < tokens.Count; i++)
            {
                var item = tokens[i];
                Print(item);
                if (i < tokens.Count - 1)
                    Print(" | ");
            }
            stringBuilder.Append(" ) ");
        }

        public void Print(Token token)
        {
            stringBuilder.Append(token.ToString());
        }

        private void Print(List<Stmt.Function> methods)
        {
            foreach (var item in methods)
            {
                item.Accept(this);
            }
        }

        public void Print(string str)
        {
            stringBuilder.Append(str);
        }

        public Object Visit(Expr.Binary expr)
        {
            Print(expr.op);
            Print(" ");
            Print(expr.left);
            Print(" ");
            Print(expr.right);

            return null;
        }

        public Object Visit(Expr.Call expr)
        {
            Print("call ");
            Print(expr.callee);
            Print(expr.arguments);

            return null;
        }

        public Object Visit(Expr.Get expr)
        {
            Print(expr.name);
            if (expr.targetObj != null)
                Print(expr.targetObj);

            return null;
        }

        public Object Visit(Expr.Grouping expr)
        {
            if (expr.expressions.Count > 0)
            {
                Print("( ");
                Print(expr.expressions);
                Print(" )");
            }
            return null;
        }

        public Object Visit(Expr.Literal expr)
        {
            Print(expr.value.ToString());
            return null;
        }

        public Object Visit(Expr.Logical expr)
        {
            Print(expr.op);
            Print(" ");
            Print(expr.left);
            Print(expr.right);
            return null;
        }

        public Object Visit(Expr.Set expr)
        {
            if(expr.targetObj != null)
                Print(expr.targetObj);
            else
                Print("assign ");
            
            Print(expr.name);
            Print(expr.val);
            return null;
        }

        public Object Visit(Expr.Super expr)
        {
            Print("super ");
            Print(expr.method);
            return null;
        }

        public Object Visit(Expr.This expr)
        {
            Print(expr.keyword);
            return null;
        }

        public Object Visit(Expr.Unary expr)
        {
            Print(expr.op);
            Print(expr.right);
            return null;
        }

        public void Visit(Stmt.Block stmt)
        {
            Print(stmt.statements);
        }

        public void Visit(Stmt.Class stmt)
        {
            Print("class ");
            Print(stmt.name);
            if (stmt.superclass != null)
            {
                Print(" inherit ");
                Print(stmt.superclass);
            }
            Indent();
            if (stmt.metaFields.Count > 0 || stmt.metaMethods.Count > 0)
            {
                PrintLine();
                Print(" meta ");
                foreach (var item in stmt.metaFields)
                {
                    Print(item);
                }
                Print(stmt.metaMethods);
            }
            if (stmt.fields.Count > 0 || stmt.methods.Count > 0)
            {
                PrintLine();
                Print(" instance ");
                foreach (var item in stmt.fields)
                {
                    Print(item);
                }
                Print(stmt.methods);
            }
            Dent();
        }

        public void Visit(Stmt.Expression stmt)
        {
            Print(stmt.expression);
        }

        public void Visit(Stmt.Function stmt)
        {
            if (indent != 0)
                PrintLine();

            Print("fun ");
            Print(stmt.name);
            Print(stmt.function);
        }

        public Object Visit(Expr.Function expr)
        {
            if (expr.parameters?.Count > 0)
                Print(expr.parameters);
            Indent();
            Print(expr.body);
            Dent();
            return null;
        }

        public void Visit(Stmt.If stmt)
        {
            Print("if ");
            Indent();
            Print(stmt.condition);
            Print("then ");
            Print(stmt.thenBranch);
            Print("else ");
            Print(stmt.elseBranch);
            Dent();
        }

        public void Visit(Stmt.Return stmt)
        {
            Print("return ");
            Print(stmt.retVals);
        }

        public void Visit(Stmt.Var stmt)
        {
            Print("var ");
            Print(stmt.name);
            Print(stmt.initializer);
        }

        public void Visit(Stmt.While stmt)
        {
            Print("while ");
            Print(stmt.condition);
            Indent();
            PrintLine();
            Print(stmt.body);
            if (stmt.increment != null)
            {
                PrintLine();
                Print(stmt.increment);
            }

            Dent();
        }

        public Object Visit(Expr.Conditional expr)
        {
            Print("cond ");
            Print(expr.condition);
            Print(" ? ");
            Print(expr.ifTrue);
            Print(" : ");
            Print(expr.ifFalse);

            return null;
        }

        public void Visit(Stmt.Break stmt)
        {
            Print("break ");
        }

        public void Visit(Stmt.Continue stmt)
        {
            Print("continue ");
        }

        public void Visit(Stmt.Chain stmt)
        {
            Print(stmt.left);
            Print(stmt.right);
        }

        public void Visit(Stmt.MultiVar stmt)
        {
            Print("multivar ");
            for (int i = 0; i < stmt.names.Count; i++)
            {
                Print(stmt.names[i]);
                if (i < stmt.names.Count - 1) Print(", ");
            }
            Print(stmt.initializer);
        }

        public Object Visit(Expr.Throw expr)
        {
            Print("throw ");
            Print(expr.expr);
            return null;
        }

        public void Visit(Stmt.Test stmt)
        {
            Print("test ");
            Print(stmt.block);
        }

        public void Visit(Stmt.TestCase stmt)
        {
            Print("testcase ");
            if(stmt.valueGrouping != null)
                Print(stmt.valueGrouping);
            
            Print(stmt.block);
        }
    }
}
