using System.Collections.Generic;

namespace LolLoginQueuePosition
{
    /// <summary>
    /// Provides a sliding window implementation ontop of the default list implementation.
    /// </summary>
    /// <typeparam name="T">Type of the list elements.</typeparam>
    public class SlidingWindow<T> : List<T>
    {
        public int PoolSize { get; }

        public bool IsSaturated => Count == PoolSize;

        public SlidingWindow(int poolSize)
        {
            PoolSize = poolSize;
        }

        public new void Add(T item)
        {
            if (Count >= PoolSize)
            {
                RemoveAt(0);
            }
            base.Add(item);
        }

    }
}
