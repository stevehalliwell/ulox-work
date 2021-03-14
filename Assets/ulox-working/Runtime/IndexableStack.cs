using System.Collections.Generic;

namespace ULox
{
    public class IndexableStack<T> : List<T>
    {
        public IndexableStack(IndexableStack<T> valueStack)
        {
            AddRange(valueStack);
        }

        public IndexableStack() { }

        public void Push(T t) => Add(t);
        public T Pop() { var res = this[Count - 1]; RemoveAt(Count - 1); return res; }
        public T Peek() => this[Count - 1];
        public T Peek(int down) => this[Count - 1 - down];
    }
}
