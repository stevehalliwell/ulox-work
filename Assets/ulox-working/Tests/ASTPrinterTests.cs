using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ULox;

namespace Tests
{
    public class ASTPrinterTests
    {
        [Test]
        public void ASTPrinter_OutputFixedTree_Matches()
        {
            var exprTest = new Expr.Binary(
                new Expr.Unary(
                    new Token(TokenType.MINUS, "-", null, 1, 0),
                    new Expr.Literal(123)),
                new Token(TokenType.STAR, "*", null, 1, 0),
                new Expr.Grouping(
                    new Expr.Literal(45.67)));

            var resultingString = "(* (- 123) (group 45.67))";

            var astPrinter = new ASTPrinter();

            Assert.AreEqual(astPrinter.Print(exprTest), resultingString);
        }
    }
}
