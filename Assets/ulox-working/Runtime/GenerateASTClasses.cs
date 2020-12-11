using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ULox
{
    public static class GenerateASTClasses
    {
        private static string _outputLocation = "Assets\\ulox-working\\Runtime\\";
        private static string[] _requiredExprTypes = new string[]
        {
            "Assign   : Token name, Expr value",
            "Binary   : Expr left, Token op, Expr right",
            "Grouping : Expr expression",
            "Literal  : object value",
            "Unary    : Token op, Expr right",
            "Variable : Token name",
        };
        private static string[] _requiredStmtTypes = new string[]
        {
            "Block      : List<Stmt> statements",
            "Expression : Expr expression",
            "Print      : Expr expression",
            "Var        : Token name, Expr initializer",
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

            if(useVisitorT)
            {
                acceptVisitorLine =@"            public override T Accept<T>(Visitor<T> visitor) => visitor.Visit(this);";
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
                    var varName = fieldItem.Split(' ')[1].Trim();
                    sb.AppendLine($"                this.{varName} = {varName};");
                }
                sb.AppendLine(@"            }");

                foreach (var fieldItem in fields)
                {
                    sb.AppendLine($"            public readonly {fieldItem};");
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
    }
}