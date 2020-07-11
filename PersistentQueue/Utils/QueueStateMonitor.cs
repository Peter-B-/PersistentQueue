﻿using System.Threading;

namespace PersistentQueue.Utils
{
    public sealed class QueueStateMonitor
    {
        private QueueState _currentState;

        private QueueStateMonitor(long initialTailIndex)
        {
            _currentState = new QueueState(initialTailIndex);
        }

        public static QueueStateMonitor Initialize(long tailIndex)
        {
            return new QueueStateMonitor(tailIndex);
        }

        public void Update(long newTailIndex)
        {
            QueueState oldState;
            QueueState newState;
            do
            {
                oldState = _currentState;
                newState = new QueueState(newTailIndex);
            } while (Interlocked.CompareExchange(ref _currentState, newState, oldState) != oldState);
            
            oldState?.Update(newState);
        }

        public IQueueState GetCurrent()
        {
            return _currentState;
        }
    }
}