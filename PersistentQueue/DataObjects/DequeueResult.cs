using System;
using System.Collections.Generic;
using PersistentQueue.Interfaces;

namespace PersistentQueue.DataObjects
{
    internal class DequeueResult : IDequeueResult
    {
        private readonly ItemRange _itemRange;
        private readonly Action<ItemRange> _commitCallBack;
        private readonly Action<ItemRange> _rejectCallBack;

        public DequeueResult(List<Memory<byte>> data, ItemRange itemRange, Action<ItemRange> commitCallBack, Action<ItemRange> rejectCallBack)
        {
            _itemRange = itemRange;
            _commitCallBack = commitCallBack;
            _rejectCallBack = rejectCallBack;
            Data = data;
        }

        public IReadOnlyList<Memory<byte>> Data { get; }

        public void Commit()
        {
            _commitCallBack(_itemRange);
        }

        public void Reject()
        {
            _rejectCallBack(_itemRange);
        }
    }
}