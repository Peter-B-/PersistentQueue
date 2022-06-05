using System;

namespace Persistent.Queue.Cache;

internal interface ICache<in TKey, TValue>: IDisposable
{
    TValue GetOrCreate(TKey key, Func<TValue> factory);
    bool TryRemoveValue(TKey key, out TValue value);

    void Release(TKey key);
}