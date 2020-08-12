using System;

namespace Persistent.Queue.Cache
{
    internal interface ICache<in TKey, TValue>: IDisposable where TValue : IDisposable
    {
        TValue this[TKey key] { get; set; }
        void Add(TKey key, TValue value);
        bool TryGetValue(TKey key, out TValue value);
        TValue Get(TKey key);
        void Remove(TKey key);
        void RemoveAll();
        bool ContainsKey(TKey key);
        void Release(TKey key);
    }
}
