namespace ULox
{
    public class FastStack<T>
    {
        private const int StartingSize = 16;
        private T[] _array = new T[StartingSize];
        private int _back = -1;

        public void Push(T val)
        {
            if (_back >= _array.Length-1)
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