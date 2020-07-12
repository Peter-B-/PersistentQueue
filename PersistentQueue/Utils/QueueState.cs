﻿using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PersistentQueue.Utils
{
    public sealed class QueueState:IQueueState
    {
        private readonly TaskCompletionSource<IQueueState> _updateTcs = new TaskCompletionSource<IQueueState>(); 
        
        public QueueState(long tailIndex)
        {
            TailIndex = tailIndex;
        }

        public void Update(IQueueState newState)
        {
            _updateTcs.TrySetResult(newState);
        }
        
        public long TailIndex { get; }
        public Task<IQueueState> NextUpdate => _updateTcs.Task;
    }
}