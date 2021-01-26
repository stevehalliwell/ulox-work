namespace ULox
{
    public interface IFunction : ICallable
    {
        bool IsGetter { get; }
    }
}
