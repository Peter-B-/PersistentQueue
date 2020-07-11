using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PersistentQueue
{
    public class PersistentQueue : IDisposable
    {
        private static readonly string MetaPageFolder = "meta";
        private static readonly string IndexPageFolder = "index";
        private static readonly string DataPageFolder = "data";

        // Index pages
        private static readonly long IndexItemsPerPage = 50000;
        private static readonly long DefaultDataPageSize = 128 * 1024 * 1024;
        private static readonly long MinimumDataPageSize = 32 * 1024 * 1024;

        // Data pages
        private readonly long DataPageSize;
        private readonly long IndexItemSize;
        private readonly long IndexPageSize;

        private readonly long MetaDataItemSize;

        // Folders
        protected readonly string QueuePath;
        private IPageFactory _dataPageFactory;

        // Head info
        private long _headDataPageIndex;
        private long _headIndexPageIndex;
        private IPageFactory _indexPageFactory;

        private readonly object _lockObject = new object();

        // MetaData
        private MetaData _metaData;
        private IPageFactory _metaPageFactory;
        private long _tailDataItemOffset;

        // Tail info
        private long _tailDataPageIndex;

        public PersistentQueue(string queuePath) : this(queuePath, DefaultDataPageSize)
        {
        }

        public PersistentQueue(string queuePath, long pageSize)
        {
            QueuePath = queuePath;
            DataPageSize = pageSize;

            MetaDataItemSize = MetaData.Size();
            IndexItemSize = IndexItem.Size();
            IndexPageSize = IndexItemSize * IndexItemsPerPage;

            Init();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Init()
        {
            // Init page factories
            _metaPageFactory =
                new PageFactory(MetaDataItemSize,
                    Path.Combine(QueuePath, MetaPageFolder)); // Page size = item size => only 1 item possible.
            _indexPageFactory = new PageFactory(IndexPageSize, Path.Combine(QueuePath, IndexPageFolder));
            _dataPageFactory = new PageFactory(DataPageSize, Path.Combine(QueuePath, DataPageFolder));

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

            var prevHeadIndex = GetPreviousIndex(_metaData.HeadIndex);
            var prevHeadIndexItem = GetIndexItem(prevHeadIndex);
            _headDataPageIndex = prevHeadIndexItem.DataPageIndex;
            _headIndexPageIndex = GetIndexPageIndex(GetPreviousIndex(_metaData.HeadIndex));
        }

        private long GetIndexPageIndex(long index)
        {
            return index / IndexItemsPerPage;
        }

        private long GetIndexItemOffset(long index)
        {
            return index % IndexItemsPerPage * IndexItemSize;
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
                PersistMetaData();
            }
        }

        public Stream Dequeue()
        {
            lock (_lockObject)
            {
                if (_metaData.HeadIndex == _metaData.TailIndex) // Head cought up with tail. Queue is empty.
                    return null; // return null or Stream.Null?

                // Delete previous index page if we are moving along to the next
                if (GetIndexPageIndex(_metaData.HeadIndex) != _headIndexPageIndex)
                {
                    _indexPageFactory.DeletePage(_headIndexPageIndex);
                    _headIndexPageIndex = GetIndexPageIndex(_metaData.HeadIndex);
                }

                // Get index item for head index
                var indexItem = GetIndexItem(_metaData.HeadIndex);

                // Delete previous data page if we are moving along to the next
                if (indexItem.DataPageIndex != _headDataPageIndex)
                {
                    _dataPageFactory.DeletePage(_headDataPageIndex);
                    _headDataPageIndex = indexItem.DataPageIndex;
                }

                // Get data page
                var dataPage = _dataPageFactory.GetPage(indexItem.DataPageIndex);

                // Get read stream
                var memoryStream = new MemoryStream();
                using (var readStream = dataPage.GetReadStream(indexItem.ItemOffset, indexItem.ItemLength))
                {
                    readStream.CopyTo(memoryStream, 4 * 1024);
                    memoryStream.Position = 0;
                }

                _dataPageFactory.ReleasePage(dataPage.Index);

                // Update meta data
                if (_metaData.HeadIndex == long.MaxValue)
                    _metaData.HeadIndex = 0;
                else
                    _metaData.HeadIndex++;

                PersistMetaData();

                return memoryStream;
            }
        }

        public Task<IDequeueResult> DequeueAsync(int maxElements)
        {
            return Task.FromResult(new DequeueResult(new List<Memory<byte>>()) as IDequeueResult);
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