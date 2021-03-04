namespace ULox
{
    public class Instance : Environment
    {
        private Class _class;
        public Class Class => _class;

        public Instance(Class @class, IEnvironment enclosing)
            : base(enclosing)
        {
            _class = @class;
        }

        public virtual void Set(string name, object val)
        {
            Assign(name, val, true, false);
        }

        public override string ToString() => $"<inst {_class.Name}>";

        public Function GetOperator(TokenType tokenType)
        {
            string operatorName = null;
            
            switch (tokenType)
            {
            case TokenType.BANG_EQUAL:
                operatorName = "_bang_equal";
                break;
            case TokenType.EQUALITY:
                operatorName = "_equality";
                break;
            case TokenType.GREATER:
                operatorName = "_greater";
                break;
            case TokenType.GREATER_EQUAL:
                operatorName = "_greater_equal";
                break;
            case TokenType.LESS:
                operatorName = "_less";
                break;
            case TokenType.LESS_EQUAL:
                operatorName = "_less_equal";
                break;
            case TokenType.MINUS:
                operatorName = "_minus";
                break;
            case TokenType.PLUS:
                operatorName = "_add";
                break;
            case TokenType.SLASH:
                operatorName = "_slash";
                break;
            case TokenType.STAR:
                operatorName = "_star";
                break;
            case TokenType.PERCENT:
                operatorName = "_percent";
                break;
            //case TokenType.ASSIGN:
            //    operatorName = "_assign";
            //    break;
            //case TokenType.BANG:
            //    operatorName = "_bang";
            //    break;
            //case TokenType.BANG_EQUAL:
            //    operatorName = "_bang_equal";
            //    break;
            //case TokenType.EQUALITY:
            //    operatorName = "_equality";
            //    break;
            //case TokenType.GREATER:
            //    operatorName = "_greater";
            //    break;
            //case TokenType.GREATER_EQUAL:
            //    operatorName = "_greater_equal";
            //    break;
            //case TokenType.LESS:
            //    operatorName = "_less";
            //    break;
            //case TokenType.LESS_EQUAL:
            //    operatorName = "_less_equal";
            //    break;
            //default:
            //    throw new LoxException($"{tokenType} is not supported in operator overloading");
            //    break;
            }

            var op = FetchNoThrow(operatorName, false, null);
            if (op != null)
                return op as Function;

            op = _class.FetchNoThrow(operatorName, false, null);
            if (op != null)
                return op as Function;
            
            return null;
        }
    }
}
