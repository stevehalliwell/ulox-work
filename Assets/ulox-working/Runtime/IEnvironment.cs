namespace ULox
{
    public struct EnvironmentVariableLocation
    {
        public ushort depth;
        public short slot;
    }

    public interface IEnvironment
    {
        IEnvironment Enclosing { get; }
        void AssignSlot(short slot, object val);
        short Define(string name, object value);
        short FindSlot(string name);
        short FindSlot(Token name);
        object FetchObject(short slot);
    }

    public static class IEnvironmentExt
    {
        public static IEnvironment Ancestor(this IEnvironment env, ushort depth)
        {
            while (depth > 0) { env = env.Enclosing; depth--; }

            return env;
        }

        public static object Fetch(this IEnvironment env, EnvironmentVariableLocation loc)
        {
            return env.Ancestor(loc.depth).FetchObject(loc.slot);
        }

        public static void Assign(this IEnvironment env, Token name, object val)
        {
            var loc = env.FindLocation(name);
            env.Ancestor(loc.depth).AssignSlot(loc.slot, val);
        }
        public static EnvironmentVariableLocation FindLocation(this IEnvironment env, string name)
        {
            short slot = -1;
            ushort depth = 0;
            while (env != null)
            {
                slot = env.FindSlot(name);
                if (slot != -1)
                    return new EnvironmentVariableLocation()
                    {
                        depth = depth,
                        slot = slot,
                    };

                env = env.Enclosing;
                depth++;
            }

            throw new LoxException($"Undefined variable {name}");
        }

        public static EnvironmentVariableLocation FindLocation(this IEnvironment env, Token name)
        {
            short slot = -1;
            ushort depth = 0;
            while (env != null)
            {
                slot = env.FindSlot(name.Lexeme);
                if (slot != -1)
                    return new EnvironmentVariableLocation()
                    {
                        depth = depth,
                        slot = slot,
                    };

                env = env.Enclosing;
                depth++;
            }

            throw new EnvironmentException(name, $"Undefined variable {name.Lexeme}");
        }
    }
}
