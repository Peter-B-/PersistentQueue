using System;
using System.Diagnostics.CodeAnalysis;

namespace Persistent.Queue.Cache;

internal interface ICache<in TKey, TValue>: IDisposable
{
    TValue GetOrCreate(TKey key, Func<TValue> factory);
    

    bool TryRemoveValue(TKey key,[NotNullWhen(true)] out TValue? value);

    void Release(TKey key);
}