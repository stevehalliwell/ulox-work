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

        public object GetMethod(Token name)
        {
            var method = _class?.FindMethod(name.Lexeme);
            if (method != null) return method.Bind(this);

            throw new InstanceException(name, "Undefined method '" + name.Lexeme + "'.");
        }

        public virtual void Set(string name, object val)
        {
            if (valueIndicies.TryGetValue(name, out var index))
            {
                objectList[index] = val;
            }
            else
            {
                DefineInAvailableSlot(name, val);
            }
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

            return _class.FindMethod(operatorName);
        }
    }
}
