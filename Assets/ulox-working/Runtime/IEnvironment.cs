using System;

namespace ULox
{
    public struct EnvironmentVariableLocation
    {
        public const short InvalidSlot = -1;
        public static readonly EnvironmentVariableLocation Invalid = 
            new EnvironmentVariableLocation { depth = ushort.MaxValue, slot = InvalidSlot };
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
        IEnvironment Enclosing { get; set; }
        void AssignSlot(short slot, object val);
        void DefineSlot(string name, short slot, object value);
        short DefineInAvailableSlot(string name, object value);
        short FindSlot(string name);
        object FetchObject(short slot);
        void ForEachValueName(Action<string> action);
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
            short slot;
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
