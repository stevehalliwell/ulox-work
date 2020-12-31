namespace ULox
{
    public interface IEnvironment
    {
        IEnvironment Enclosing { get; }
        void Assign(string tokenLexeme, object val, bool checkEnclosing);
        void Assign(Token name, object val, bool checkEnclosing);
        void Define(string name, object value);
        bool Exists(string address);
        object Fetch(string tokenLexeme, bool checkEnclosing);
        object Fetch(Token name, bool checkEnclosing);
    }

    public static class IEnvironmentExt
    {
        public static IEnvironment Ancestor(this IEnvironment environment, int distance)
        {
            for (int i = 0; i < distance; i++)
                environment = environment.Enclosing;

            return environment;
        }

        public static void AssignAt(this IEnvironment environment, int distance, Token name, object val)
        {
            environment.Ancestor(distance).Assign(name, val, false);
        }

        public static object FetchAncestor(this IEnvironment environment, int distance, Token name)
        {
            return environment.Ancestor(distance).Fetch(name, false);
        }

        public static object FetchAncestor(this IEnvironment environment, int distance, string nameLexeme)
        {
            return environment.Ancestor(distance).Fetch(nameLexeme, false);
        }

        public static IEnvironment GetChildEnvironment(this IEnvironment environment, string name)
        {
            return environment.Fetch(name, false) as IEnvironment;
        }
    }
}
