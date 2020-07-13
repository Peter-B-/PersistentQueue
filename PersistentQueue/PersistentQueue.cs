using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Persistent.Queue.DataObjects;
using Persistent.Queue.Interfaces;
using Persistent.Queue.Interfaces.Intern;
using Persistent.Queue.Utils;

namespace Persistent.Queue
{
    public class PersistentQueue : IPersistentQueue
    {
        // Page factories
        private readonly IPageFactory _dataPageFactory;

        // Data pages
        private readonly long _dataPageSize;
        private readonly long _indexItemSize;
        private readonly IPageFactory _indexPageFactory;
        private readonly object _lockObject = new object();
        private readonly long _metaDataItemSize;
        private readonly IPageFactory _metaPageFactory;

        private readonly QueueStateMonitor _queueMonitor;
        protected readonly PersistentQueueConfiguration Configuration;

        // MetaData
        private MetaData _metaData;
        private long _tailDataItemOffset;

        // Tail info
        private long _tailDataPageIndex;

        public PersistentQueue(string queuePath) : this(PersistentQueueConfiguration.GetDefault(queuePath))
        {
        }

        public PersistentQueue(string queuePath, long dataPageSize) : this(
            PersistentQueueConfiguration.GetDefault(queuePath, dataPageSize))
        {
        }

        public PersistentQueue(PersistentQueueConfiguration configuration)
        {
            Configuration = configuration;

            _metaDataItemSize = MetaData.Size();
            _indexItemSize = IndexItem.Size();
            var indexPageSize = _indexItemSize * configuration.IndexItemsPerPage;
            _dataPageSize = Configuration.DataPageSize;

            // Init page factories
            // MetaPage: Page size = item size => only 1 item possible.
            _metaPageFactory = new PageFactory(_metaDataItemSize, Configuration.GetMetaPath());
            _indexPageFactory = new PageFactory(indexPageSize, Configuration.GetIndexPath());
            _dataPageFactory = new PageFactory(_dataPageSize, Configuration.GetDataPath());

            InitializeMetaData();

            _queueMonitor = QueueStateMonitor.Initialize(_metaData.TailIndex);
        }

        public bool HasItems => _metaData.TailIndex - _metaData.HeadIndex > 0;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Enqueue(ReadOnlySpan<byte> itemData)
        {
            lock (_lockObject)
            {
                // Throw or silently return if itemData is null?
                if (itemData == null)
                    return;

                if (itemData.Length > _dataPageSize)
                    throw new ArgumentOutOfRangeException("Item data length is greater than queue data page size");

                if (_tailDataItemOffset + itemData.Length > _dataPageSize) // Not enough space in current page
                {
                    _tailDataPageIndex++;
                    _tailDataItemOffset = 0;
                }

                // Get data page
                var dataPage = _dataPageFactory.GetPage(_tailDataPageIndex);

                // Get write stream
                using (var writeStream = dataPage.GetWriteStream(_tailDataItemOffset, itemData.Length))
                {
                    // Write data to write stream
                    writeStream.Write(itemData);
                }

                // Release our reference to the data page
                _dataPageFactory.ReleasePage(_tailDataPageIndex);

                // Udate index
                // Get index page
                var indexPage = _indexPageFactory.GetPage(GetIndexPageIndex(_metaData.TailIndex));

                // Get write stream
                using (var writeStream =
                    indexPage.GetWriteStream(GetIndexItemOffset(_metaData.TailIndex), _indexItemSize))
                {
                    var indexItem = new IndexItem
                    {
                        DataPageIndex = _tailDataPageIndex,
                        ItemOffset = _tailDataItemOffset,
                        ItemLength = itemData.Length
                    };
                    indexItem.WriteToStream(writeStream);
                }

                _indexPageFactory.ReleasePage(GetIndexPageIndex(_metaData.TailIndex));

                // Advance
                _tailDataItemOffset += itemData.Length;

                // Update meta data
                _metaData.TailIndex++;

                _queueMonitor.Update(_metaData.TailIndex);
                PersistMetaData();
            }
        }

        public async Task<IDequeueResult> DequeueAsync(int maxElements, int minElements = 1)
        {
            var queueState = _queueMonitor.GetCurrent();

            var headIndex = _metaData.HeadIndex;

            while (queueState.TailIndex - headIndex < minElements)
                queueState = await queueState.NextUpdate.ConfigureAwait(false);

            var availableElements = queueState.TailIndex - headIndex;
            var noOfItems = (int) Math.Min(availableElements, maxElements);

            var data =
                Enumerable.Range(0, noOfItems)
                    .Select(offset => headIndex + offset)
                    .Select(ReadItem)
                    .ToList();


            var result = new DequeueResult(data, new ItemRange(headIndex, noOfItems), CommitBatch, RejectBatch);
            return result;
        }

        private void InitializeMetaData()
        {
            var metaPage = _metaPageFactory.GetPage(0);
            using (var readStream = metaPage.GetReadStream(0, _metaDataItemSize))
            {
                _metaData = MetaData.ReadFromStream(readStream);
            }

            // Update local data pointers from previously persisted index item
            var prevTailIndex = GetPreviousIndex(_metaData.TailIndex);
            var prevTailIndexItem = GetIndexItem(prevTailIndex);
            _tailDataPageIndex = prevTailIndexItem.DataPageIndex;
            _tailDataItemOffset = prevTailIndexItem.ItemOffset + prevTailIndexItem.ItemLength;
        }

        private long GetIndexPageIndex(long index)
        {
            return index / Configuration.IndexItemsPerPage;
        }

        private long GetIndexItemOffset(long index)
        {
            return index % Configuration.IndexItemsPerPage * _indexItemSize;
        }

        private long GetPreviousIndex(long index)
        {
            if (index > 0)
                return index - 1;

            return index;
        }

        private IndexItem GetIndexItem(long index)
        {
            IndexItem indexItem;

            var indexPage = _indexPageFactory.GetPage(GetIndexPageIndex(index));
            using (var stream = indexPage.GetReadStream(GetIndexItemOffset(index), _indexItemSize))
            {
                indexItem = IndexItem.ReadFromStream(stream);
            }

            _indexPageFactory.ReleasePage(indexPage.Index);

            return indexItem;
        }

        private void PersistMetaData()
        {
            var metaPage = _metaPageFactory.GetPage(0);
            using (var writeStream = metaPage.GetWriteStream(0, _metaDataItemSize))
            {
                _metaData.WriteToStream(writeStream);
            }
        }

        private void RejectBatch(ItemRange range)
        {
        }

        private void CommitBatch(ItemRange range)
        {
            var newHeadIndex = range.HeadIndex + range.ItemCount;
            long oldHeadIndex;

            lock (_lockObject)
            {
                // Update meta data
                oldHeadIndex = _metaData.HeadIndex;
                if (newHeadIndex > oldHeadIndex)
                    _metaData.HeadIndex = newHeadIndex;

                PersistMetaData();
            }

            if (newHeadIndex > oldHeadIndex)
            {
                var lastHeadIndex = GetPreviousIndex(oldHeadIndex);
                var lastWrittenIndex = GetPreviousIndex(newHeadIndex);
                var oldHeadIndexItem = GetIndexItem(lastHeadIndex);
                var lastWrittenIndexItem = GetIndexItem(lastWrittenIndex);

                for (var dataPageIndex = oldHeadIndexItem.DataPageIndex;
                    dataPageIndex < lastWrittenIndexItem.DataPageIndex;
                    dataPageIndex++)
                    _dataPageFactory.DeletePage(dataPageIndex);

                for (var indexPageIndex = GetIndexPageIndex(lastHeadIndex);
                    indexPageIndex < GetIndexPageIndex(lastWrittenIndex);
                    indexPageIndex++)
                    _indexPageFactory.DeletePage(indexPageIndex);
            }
        }

        private Memory<byte> ReadItem(long itemIndex)
        {
            // Get index item for head index
            var indexItem = GetIndexItem(itemIndex);

            // Get data page
            var dataPage = _dataPageFactory.GetPage(indexItem.DataPageIndex);

            // Get read stream
            // Todo: Optimize: Remove copy operation
            var buffer = new byte[indexItem.ItemLength];
            using (var memoryStream = new MemoryStream(buffer))
            using (var readStream = dataPage.GetReadStream(indexItem.ItemOffset, indexItem.ItemLength))
            {
                readStream.CopyTo(memoryStream, 4 * 1024);
                memoryStream.Position = 0;
            }

            _dataPageFactory.ReleasePage(dataPage.Index);

            return buffer;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _metaPageFactory?.Dispose();
                _indexPageFactory?.Dispose();
                _dataPageFactory?.Dispose();
            }
        }
    }
}