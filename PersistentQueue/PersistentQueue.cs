using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PersistentQueue.Utils;

namespace PersistentQueue
{
    public class PersistentQueue : IDisposable
    {
        protected readonly PersistentQueueConfiguration Configuration;
        private readonly object _lockObject = new object();

        private readonly QueueStateMonitor _queueMonitor;

        // Data pages
        private readonly long DataPageSize;
        private readonly long IndexItemSize;
        private readonly long IndexPageSize;

        private readonly long MetaDataItemSize;

        // Folders
        private IPageFactory _dataPageFactory;
        private IPageFactory _indexPageFactory;

        // MetaData
        private MetaData _metaData;
        private IPageFactory _metaPageFactory;
        private long _tailDataItemOffset;

        // Tail info
        private long _tailDataPageIndex;

        public PersistentQueue(string queuePath) : this(PersistentQueueConfiguration.GetDefault(queuePath))
        {
        }

        public PersistentQueue(string queuePath, long dataPageSize) : this(PersistentQueueConfiguration.GetDefault(queuePath, dataPageSize))
        {
        }

        public PersistentQueue(PersistentQueueConfiguration configuration)
        {
            Configuration = configuration;

            MetaDataItemSize = MetaData.Size();
            IndexItemSize = IndexItem.Size();
            IndexPageSize = IndexItemSize * configuration.IndexItemsPerPage;
            DataPageSize = Configuration.DataPageSize;
            
            Init();

            _queueMonitor = QueueStateMonitor.Initialize(_metaData.TailIndex);
        }

        public bool HasItems => (_metaData.TailIndex - _metaData.HeadIndex) > 0;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Init()
        {
            // Init page factories
            // MetaPage: Page size = item size => only 1 item possible.
            _metaPageFactory = new PageFactory(MetaDataItemSize, Configuration.GetMetaPath());
            _indexPageFactory = new PageFactory(IndexPageSize, Configuration.GetIndexPath());
            _dataPageFactory = new PageFactory(DataPageSize, Configuration.GetDataPath());

            InitializeMetaData();
        }

        private void InitializeMetaData()
        {
            var metaPage = _metaPageFactory.GetPage(0);
            using (var readStream = metaPage.GetReadStream(0, MetaDataItemSize))
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
            return index %  Configuration.IndexItemsPerPage * IndexItemSize;
        }

        private long GetPreviousIndex(long index)
        {
            // TODO: Handle wrap situations => index == long.MaxValue
            if (index > 0)
                return index - 1;

            return index;
        }

        private IndexItem GetIndexItem(long index)
        {
            IndexItem indexItem;

            var indexPage = _indexPageFactory.GetPage(GetIndexPageIndex(index));
            using (var stream = indexPage.GetReadStream(GetIndexItemOffset(index), IndexItemSize))
            {
                indexItem = IndexItem.ReadFromStream(stream);
            }

            _indexPageFactory.ReleasePage(indexPage.Index);

            return indexItem;
        }

        private void PersistMetaData()
        {
            var metaPage = _metaPageFactory.GetPage(0);
            using (var writeStream = metaPage.GetWriteStream(0, MetaDataItemSize))
            {
                _metaData.WriteToStream(writeStream);
            }
        }

        public void Enqueue(ReadOnlySpan<byte> itemData)
        {
            lock (_lockObject)
            {
                // Throw or silently return if itemData is null?
                if (itemData == null)
                    return;

                if (itemData.Length > DataPageSize)
                    throw new ArgumentOutOfRangeException("Item data length is greater than queue data page size");

                if (_tailDataItemOffset + itemData.Length > DataPageSize) // Not enough space in current page
                {
                    if (_tailDataPageIndex == long.MaxValue)
                        _tailDataPageIndex = 0;
                    else
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
                    indexPage.GetWriteStream(GetIndexItemOffset(_metaData.TailIndex), IndexItemSize))
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
                if (_metaData.TailIndex == long.MaxValue)
                    _metaData.TailIndex = 0;
                else
                    _metaData.TailIndex++;

                _queueMonitor.Update(_metaData.TailIndex);
                PersistMetaData();
            }
        }

        public async Task<IDequeueResult> DequeueAsync(int maxElements)
        {
            var queueState = _queueMonitor.GetCurrent();

            var headIndex = _metaData.HeadIndex;

            while (headIndex == queueState.TailIndex)
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
                var oldHeadIndexItem = GetIndexItem(oldHeadIndex);
                var newHeadIndexItem = GetIndexItem(newHeadIndex);

                // Delete previous data page if we are moving along to the next
                for (var dataPageIndex = oldHeadIndexItem.DataPageIndex; dataPageIndex < newHeadIndexItem.DataPageIndex; dataPageIndex++)
                    _dataPageFactory.DeletePage(dataPageIndex);

                // Delete previous index page if we are moving along to the next
                for (var indexPageIndex = GetIndexPageIndex(oldHeadIndex); indexPageIndex < GetIndexPageIndex(newHeadIndex); indexPageIndex++)
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