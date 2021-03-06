namespace ULox
{
    public interface IEnvironment
    {
        IEnvironment Enclosing { get; set; }
        void Assign(string tokenLexeme, object val, bool canDefine, bool checkEnclosing);
        void Define(string name, object value);
        bool Exists(string address);
        object Fetch(string tokenLexeme, bool checkEnclosing);
        object FetchNoThrow(string tokenLexeme, bool checkEnclosing, object defaultIfNotFound);
        void VisitValues(System.Action<string, object> action);
        void Reset(IEnvironment currentEnvironment);
    }

    public static class IEnvironmentExt
    {
        public static void AssignIfExists(this IEnvironment environment, string name, object val)
        {
            if (environment.Exists(name)) environment.Assign(name, val, false, false);
        }
    }
}
