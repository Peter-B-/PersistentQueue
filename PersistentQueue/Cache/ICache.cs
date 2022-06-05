using System.Diagnostics.CodeAnalysis;

namespace Persistent.Queue.Cache;

internal interface ICache<in TKey, TValue> : IDisposable
{
    TValue GetOrCreate(TKey key, Func<TValue> factory);

    void Release(TKey key);


    bool TryRemoveValue(TKey key, [NotNullWhen(true)] out TValue? value);
}
