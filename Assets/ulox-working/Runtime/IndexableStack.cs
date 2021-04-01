using System;
using System.Collections.Generic;

namespace ULox
{
    //todo switching to a more simple and strict vector style container may improve perf 
    public class IndexableStack<T> : List<T>
    {
        public IndexableStack(IndexableStack<T> valueStack)
        {
            AddRange(valueStack);
        }

        public IndexableStack() { }

        public void Push(T t) => Add(t);
        public T Pop() { var res = this[Count - 1]; RemoveAt(Count - 1); return res; }
        public T Peek() => Peek(0);
        public T Peek(int down)
        {
            if (Count == 0) return default;

            return this[Count - 1 - down];
        }
    }

    public class FastStack<T>
    {
        private const int StartingSize = 16;
        private T[] _array = new T[StartingSize];
        private int _back = -1;

        public void Push(T val)
        {
            if (_back >= _array.Length)
                System.Array.Resize(ref _array, _array.Length * 2);

            _array[++_back] = val;
        }

        public T Pop()
        {
            return _array[_back--];
        }

        public T Peek(int down = 0)
        {
            return _array[_back-down];
        }

        public int Count => _back+1;

        public void SetAt(int index, T t)
        {
            _array[index] = t;
        }

        public T this[int index] { get => _array[index]; set => _array[index] = value; }
    }
}