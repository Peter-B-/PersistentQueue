using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Persistent.Queue.Cache
{
    internal class Cache<TKey, TValue> : ICache<TKey, TValue> where TValue : IDisposable
    {
        private static readonly int DefaultTtl = 10 * 1000;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Dictionary<TKey, TValue> _items;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly Dictionary<TKey, TTLValue> _ttlDict;
        private readonly int _ttl;

        public Cache() : this(DefaultTtl)
        {
        }

        public Cache(int ttlMillis)
        {
            _ttl = ttlMillis;
            _items = new Dictionary<TKey, TValue>();
            _ttlDict = new Dictionary<TKey, TTLValue>();
            Task.Factory.StartNew(CleanupLoop, _cts.Token);
        }

        public void Add(TKey key, TValue value)
        {
            try
            {
                _lock.EnterWriteLock();
                _items.Add(key, value);

                _ttlDict.Add(key, new TTLValue
                {
                    LastAccessTimestamp = DateTime.Now.Ticks,
                    RefCount = 1
                });
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = Get(key);
            if (value != null)
                return true;

            return false;
        }

        public TValue Get(TKey key)
        {
            try
            {
                _lock.EnterReadLock();

                TTLValue ttl;
                if (_ttlDict.TryGetValue(key, out ttl))
                {
                    Interlocked.Exchange(ref ttl.LastAccessTimestamp, DateTime.Now.Ticks);
                    Interlocked.Increment(ref ttl.RefCount);
                }

                TValue item;
                if (_items.TryGetValue(key, out item))
                    return item;

                return default;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public TValue this[TKey key]
        {
            get => Get(key);
            set => Add(key, value);
        }

        public void Remove(TKey key)
        {
            try
            {
                _lock.EnterWriteLock();

                TValue item;
                if (_items.TryGetValue(key, out item))
                    item.Dispose();

                _items.Remove(key);
                _ttlDict.Remove(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RemoveAll()
        {
            try
            {
                _lock.EnterWriteLock();

                foreach (var item in _items.Values.ToArray())
                    if (item != null)
                        item.Dispose();

                _items.Clear();
                _ttlDict.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool ContainsKey(TKey key)
        {
            try
            {
                _lock.EnterReadLock();
                return _items.ContainsKey(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Release(TKey key)
        {
            try
            {
                _lock.EnterReadLock();

                TTLValue ttl;
                if (_ttlDict.TryGetValue(key, out ttl))
                    Interlocked.Decrement(ref ttl.RefCount);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void RemoveOldItems()
        {
            var count = 0;
            try
            {
                _lock.EnterUpgradeableReadLock();

                var keysToRemove = _ttlDict
                    .Where(i => i.Value != null
                                && i.Value.RefCount <= 0
                                && i.Value.LastAccessTimestamp + _ttl < DateTime.Now.Ticks)
                    .Select(i => i.Key)
                    .ToArray();

                if (keysToRemove.Length > 0)
                    try
                    {
                        _lock.EnterWriteLock();
                        foreach (var key in keysToRemove)
                        {
                            TValue item;
                            if (_items.TryGetValue(key, out item))
                                item.Dispose();

                            _items.Remove(key);
                            _ttlDict.Remove(key);

                            count++;
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

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                RemoveAll();
                _cts?.Dispose();
                _lock?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
