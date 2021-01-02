namespace ULox
{
    public struct EnvironmentVariableLocation
    {
        public static readonly EnvironmentVariableLocation Invalid = new EnvironmentVariableLocation { depth = 0, slot = -1 };
        public ushort depth;
        public short slot;

        public static bool operator ==(EnvironmentVariableLocation left, EnvironmentVariableLocation right)
        {
            return left.depth == right.depth && left.slot == right.slot;
        }

        public static bool operator !=(EnvironmentVariableLocation left, EnvironmentVariableLocation right)
        {
            return !(left == right);
        }
    }

    public interface IEnvironment
    {
        IEnvironment Enclosing { get; }
        void AssignSlot(short slot, object val);
        short Define(string name, object value);
        short FindSlot(string name);
        object FetchObject(short slot);
    }

    public static class IEnvironmentExt
    {
        public static IEnvironment Ancestor(this IEnvironment env, ushort depth)
        {
            while (depth > 0) { env = env.Enclosing; depth--; }

            return env;
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
    }
}
