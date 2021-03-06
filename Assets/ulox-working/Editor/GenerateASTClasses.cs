﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace ULox
{
    public static class GenerateASTClasses
    {
        private static string _outputLocation = "Assets\\ulox-working\\Runtime\\Generated\\";

        private static string[] _requiredExprTypes = new string[]
        {
            "Assign   : Token name, Expr value",
            "Set      : Expr targetObj, Token name, Expr val",
            "Binary   : Expr left, Token op, Expr right",
            "Call     : Expr callee, Token paren, Expr.Grouping arguments",
            "Get      : Expr targetObj, Token name",
            "Grouping : List<Expr> expressions",
            "Variable : Token name",
            "Literal  : object value",
            "Logical  : Expr left, Token op, Expr right",
            "Unary    : Token op, Expr right",
            "Conditional : Expr condition, Expr ifTrue, Expr ifFalse",
            "Function : List<Token> parameters, List<Stmt> body",
            "Throw    : Token keyword, Expr expr",
        };

        private static string[] _requiredStmtTypes = new string[]
        {
            "Block      : List<Stmt> statements",
            "Chain      : Stmt left, Stmt right",
            "Class      : Token name, Expr.Variable superclass," +
                        " Stmt.Function init," +
                        " List<Stmt.Function> metaMethods," +
                        " List<Stmt.Var> fields," +
                        " List<Stmt.Var> metaFields",
            "Expression : Expr expression",
            "Function   : Token name, Expr.Function function",
            "If         : Expr condition, Stmt thenBranch," +
                        " Stmt elseBranch",
            "Return     : Token keyword, Expr.Grouping retVals",
            "Var        : Token name, Expr initializer",
            "MultiVar   : List<Token> names, Expr initializer",
            "While      : Expr condition, Stmt body," +
                        " Stmt increment",
            "Break      : Token keyword",
            "Continue   : Token keyword",
            "Test       : Token token, Token name, Stmt.Block block",
            "TestCase   : Token name, Expr.Grouping valueGrouping, Stmt.Block block",
        };

        [MenuItem("Create/GenerateASTClasses")]
        public static void GenerateAll()
        {
            DefineAST(_outputLocation, "Expr", _requiredExprTypes, true);
            DefineAST(_outputLocation, "Stmt", _requiredStmtTypes, false);
        }

        private static void DefineAST(
            string outputLocation,
            string rootTypeName,
            string[] requiredTypes,
            bool useVisitorT)
        {
            string acceptVisitorLine = null;
            string defineVisitorLine = null;
            string visitorReturnType = null;

            if (useVisitorT)
            {
                acceptVisitorLine = @"            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);";
                defineVisitorLine = @"
        public abstract T Accept<T>(Visitor<T> visitor);

        public interface Visitor<T>
        {";
                visitorReturnType = "T";
            }
            else
            {
                acceptVisitorLine = @"            public override void Accept(Visitor visitor) => visitor.Visit(this);";
                defineVisitorLine = @"
        public abstract void Accept(Visitor visitor);

        public interface Visitor
        {";
                visitorReturnType = "void";
            }

            var rootTypeNameLower = rootTypeName.ToLower();

            var sb = new StringBuilder();
            sb.Append($@"using System.Collections.Generic;

namespace ULox
{{
    public abstract class {rootTypeName}
    {{");

            var classNames = new List<string>();

            foreach (var item in requiredTypes)
            {
                var split = item.Split(':');
                var className = split[0].Trim();
                classNames.Add(className);
                var fields = split[1].Trim().Split(',').Select(x => x.Trim()).ToArray();

                sb.AppendLine($@"
        public class {className} : {rootTypeName}
        {{
            public {className}({split[1]})
            {{");
                foreach (var fieldItem in fields)
                {
                    var fieldItemSplits = fieldItem.Split(' ');
                    if (fieldItemSplits.Length > 1)
                    {
                        var varName = fieldItemSplits[1].Trim();
                        sb.AppendLine($"                this.{varName} = {varName};");
                    }
                }
                sb.AppendLine(@"            }");

                foreach (var fieldItem in fields)
                {
                    if (fieldItem.Length > 1)
                    {
                        if (IsReadOnlyField(fieldItem))
                            sb.AppendLine($"            public readonly {fieldItem};");
                        else
                            sb.AppendLine($"            public {fieldItem};");
                    }
                }
                sb.AppendLine(acceptVisitorLine);
                sb.Append(@"        }");
            }

            sb.AppendLine();
            sb.AppendLine(defineVisitorLine);
            foreach (var item in classNames)
            {
                sb.AppendLine($"            {visitorReturnType} Visit({item} {rootTypeNameLower});");
            }
            sb.AppendLine(@"        }
    }
}");

            System.IO.File.WriteAllText(outputLocation + rootTypeName + ".cs", sb.ToString());
        }

        private static bool IsReadOnlyField(string fieldItem)
        {
            return !(fieldItem.Contains("targetObj"));
        }
    }
}
