using System;

namespace Persistent.Queue.Cache
{
    internal interface ICache<TKey, TValue> where TValue : IDisposable
    {
        TValue this[TKey key] { get; set; }
        void Add(TKey key, TValue value);
        bool TryGetValue(TKey key, out TValue value);
        TValue Get(TKey key);
        void Remove(TKey key);
        void RemoveAll();
        bool ContainsKey(TKey key);
    }
}