namespace ULox
{
    public class Instance
    {
        private Class _class;

        public Instance(Class @class)
        {
            _class = @class;
        }

        public override string ToString()
        {
            return "<inst " + _class.Name + ">";
        }
    }
}