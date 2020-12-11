using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ULox
{
    public abstract class Expression
    {

        public class Binary : Expression
        {
                     public readonly Expr left;
         public readonly  Token operator;
         public readonly  Expr right;

        }
        public class Grouping : Expression
        {
                     public readonly Expr expression;

        }
        public class Literal : Expression
        {
                     public readonly Object value;

        }
        public class Unary : Expression
        {
                     public readonly Token operator;
         public readonly  Expr right;

        }
    }

}