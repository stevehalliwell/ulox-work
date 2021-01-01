namespace ULox
{
    public interface IEnvironment
    {
        IEnvironment Enclosing { get; }
        int Assign(string tokenLexeme, object val, bool checkEnclosing);
        void AssignIndex(int index, object val);
        int AssignT(Token name, object val, bool checkEnclosing);
        int Define(string name, object value);
        int FetchIndex(string name);
        object Fetch(string tokenLexeme, bool checkEnclosing);
        object FetchT(Token name, bool checkEnclosing);
        object FetchIndex(int index);
    }

    public static class IEnvironmentExt
    {
        public static IEnvironment Ancestor(this IEnvironment environment, int distance)
        {
            for (int i = 0; i < distance; i++)
                environment = environment.Enclosing;

            return environment;
        }

        public static int AssignAt(this IEnvironment environment, int distance, Token name, object val)
        {
            return environment.Ancestor(distance).AssignT(name, val, false);
        }

        public static object FetchAncestor(this IEnvironment environment, int distance, Token name)
        {
            return environment.Ancestor(distance).FetchT(name, false);
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
