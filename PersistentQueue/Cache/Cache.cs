using System.Diagnostics.CodeAnalysis;

namespace Persistent.Queue.Cache;

public class Cache<TKey, TValue> : ICache<TKey, TValue> where TKey : notnull
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<TKey, CacheItem> _items = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly TimeSpan _ttl;


    public Cache(TimeSpan ttl)
    {
        if (ttl > TimeSpan.FromSeconds(1))
            _ttl = ttl;
        else
            _ttl = TimeSpan.FromSeconds(1);

        Task.Factory.StartNew(CleanupLoop, _cts.Token);
    }

    public TValue GetOrCreate(TKey key, Func<TValue> factory)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            {
                if (_items.TryGetValue(key, out var item))
                {
                    item.IncreaseRefCount();
                    return item.Value;
                }
            }

            _lock.EnterWriteLock();
            try
            {
                // Retry to return item. Another process might have created it while
                // waiting for the write lock.
                if (_items.TryGetValue(key, out var itemRetry))
                {
                    itemRetry.IncreaseRefCount();
                    return itemRetry.Value;
                }

                var value = factory();
                var item = new CacheItem(key, value);
                _items.Add(key, item);

                item.IncreaseRefCount();
                return item.Value;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    public void Release(TKey key)
    {
        _lock.EnterReadLock();
        try
        {
            if (_items.TryGetValue(key, out var item))
                item.DecreaseRefCount();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool TryRemoveValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_items.TryGetValue(key, out var item))
            {
                value = item.Value!;
                _items.Remove(item.Key);
                return true;
            }

            value = default;
            return false;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }


    public void RemoveAll()
    {
        _lock.EnterWriteLock();
        try
        {
            foreach (var item in _items.Values.ToArray())
                (item.Value as IDisposable)?.Dispose();

            _items.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts.Cancel();

            RemoveAll();

            // Wait until write lock becomes available.
            // If this is the case, The CleanupLoop must have finished.
            _lock.EnterWriteLock();
            _lock.ExitWriteLock();
            _lock.Dispose();

            _cts.Dispose();
        }
    }

    private async void CleanupLoop()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                RemoveOldItems();
                await Task.Delay(_ttl / 2, _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    private void RemoveOldItems()
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            var removeTimeStamp = DateTime.Now.Subtract(_ttl);
            var itemsToRemove = _items.Values
                .Where(i =>
                           Interlocked.Read(ref i.RefCount) <= 0
                           && i.LastAccessTimestamp < removeTimeStamp)
                .ToArray();

            if (itemsToRemove.Length <= 0) return;

            _lock.EnterWriteLock();
            try
            {
                foreach (var item in itemsToRemove)
                    if (Interlocked.Read(ref item.RefCount) == 0)
                    {
                        (item.Value as IDisposable)?.Dispose();
                        _items.Remove(item.Key);
                    }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    private class CacheItem(TKey key, TValue value)
    {
        public DateTime LastAccessTimestamp = DateTime.Now;
        public long RefCount;

        public TKey Key { get; } = key;
        public TValue Value { get; } = value;

        public void DecreaseRefCount()
        {
            Interlocked.Decrement(ref RefCount);
        }

        public void IncreaseRefCount()
        {
            Interlocked.Increment(ref RefCount);
            LastAccessTimestamp = DateTime.Now;
        }
    }
}
