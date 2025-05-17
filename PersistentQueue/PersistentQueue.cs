using System.Diagnostics.CodeAnalysis;
using Persistent.Queue.DataObjects;
using Persistent.Queue.Interfaces;
using Persistent.Queue.Utils;

namespace Persistent.Queue;

public class PersistentQueue : IPersistentQueue, IPersistentQueueStatisticSource
{
    // Page factories
    private readonly PageFactory _dataPageFactory;

    private readonly long _dataPageSize;
    private readonly long _indexItemSize;
    private readonly PageFactory _indexPageFactory;
    private readonly object _lockObject = new();
    private readonly long _metaDataItemSize;
    private readonly PageFactory _metaPageFactory;

    private readonly QueueStateMonitor _queueMonitor;

    // MetaData
    private MetaData _metaData;
    private long _tailDataItemOffset;

    // Tail info
    private long _tailDataPageIndex;

    public PersistentQueue(string queuePath) : this(new PersistentQueueConfiguration(queuePath))
    {
    }

    public PersistentQueue(string queuePath, long dataPageSize) : this(
        new PersistentQueueConfiguration(queuePath, dataPageSize))
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
        _metaPageFactory = new PageFactory(_metaDataItemSize, Configuration.GetMetaPath(), TimeSpan.FromHours(1));
        _indexPageFactory = new PageFactory(indexPageSize, Configuration.GetIndexPath());
        _dataPageFactory = new PageFactory(_dataPageSize, Configuration.GetDataPath());

        InitializeMetaData();

        _queueMonitor = QueueStateMonitor.Initialize(_metaData!.TailIndex);
    }

    public bool HasItems => _metaData.TailIndex - _metaData.HeadIndex > 0;

    protected PersistentQueueConfiguration Configuration { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public Task<IDequeueResult> DequeueAsync(CancellationToken token = default)
    {
        return DequeueAsync(Configuration.MinDequeueBatchSize, Configuration.MaxDequeueBatchSize, token);
    }

    public async Task<IDequeueResult> DequeueAsync(int minItems, int maxItems, CancellationToken token = default)
    {
        if (minItems < 1) minItems = 1;
        if (maxItems < minItems) maxItems = minItems;

        var queueState = _queueMonitor.GetCurrent();

        var headIndex = _metaData.HeadIndex;

        while (queueState.TailIndex - headIndex < minItems)
            queueState = await queueState.NextUpdate
                .WaitAsync(token)
                .ConfigureAwait(false);

        var availableElements = queueState.TailIndex - headIndex;
        var noOfItems = (int) Math.Min(availableElements, maxItems);

        var data =
            Configuration.MaxDequeueBatchSizeInBytes.HasValue
                ? ReadItemsWithSizeLimit(headIndex, noOfItems, Configuration.MaxDequeueBatchSizeInBytes.Value)
                : ReadItems(headIndex, noOfItems);


        var result = new DequeueResult(data, new ItemRange(headIndex, data.Count), CommitBatch, RejectBatch);
        return result;
    }

    public void Enqueue(ReadOnlySpan<byte> itemData)
    {
        if (itemData.Length > _dataPageSize)
            throw new ArgumentOutOfRangeException(nameof(itemData),
                                                  "Item data length is greater than queue data page size");

        if (Configuration.ThrowExceptionWhenItemExceedingMaxDequeueBatchSizeIsEnqueued &&
            Configuration.MaxDequeueBatchSizeInBytes.HasValue &&
            itemData.Length > Configuration.MaxDequeueBatchSizeInBytes.Value
           )
            throw new InvalidOperationException(
                $"Item is larger than {nameof(Configuration.MaxDequeueBatchSizeInBytes)} ({itemData.Length}, {Configuration.MaxDequeueBatchSizeInBytes}) and cannot be enqueued.");

        lock (_lockObject)
        {
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

    public QueueStatistics GetStatistics()
    {
        var headIndex = _metaData.HeadIndex;
        var tailIndex = _metaData.TailIndex;

        var dataSize = GetDataSize(headIndex, tailIndex);

        return new QueueStatistics
        {
            QueueLength = tailIndex - headIndex,
            QueueDataSizeEstimate = dataSize,
            TotalEnqueuedItems = tailIndex
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        _metaPageFactory.Dispose();
        _indexPageFactory.Dispose();
        _dataPageFactory.Dispose();
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

    private long GetDataSize(long headIndex, long tailIndex)
    {
        if (headIndex == tailIndex) return 0;

        var headIndexItem = GetIndexItem(headIndex);
        var tailIndexItem = GetIndexItem(tailIndex - 1);

        var dataSize = IndexItemHelper.EstimateQueueDataSize(headIndexItem, tailIndexItem, _dataPageSize);
        return dataSize;
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

    private long GetIndexItemOffset(long index)
    {
        return index % Configuration.IndexItemsPerPage * _indexItemSize;
    }

    private long GetIndexPageIndex(long index)
    {
        return index / Configuration.IndexItemsPerPage;
    }

    private static long GetPreviousIndex(long index)
    {
        if (index > 0)
            return index - 1;

        return index;
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

    private void PersistMetaData()
    {
        var metaPage = _metaPageFactory.GetPage(0);
        using (var writeStream = metaPage.GetWriteStream(0, _metaDataItemSize))
        {
            _metaData.WriteToStream(writeStream);
        }

        _metaPageFactory.ReleasePage(0);
    }

    private ReadOnlyMemory<byte> Readitem(IndexItem indexItem)
    {
        // Get data page
        var dataPage = _dataPageFactory.GetPage(indexItem.DataPageIndex);

        // Get read stream
        // Todo: Optimize: Remove copy operation
        var buffer = new byte[indexItem.ItemLength];
        using (var readStream = dataPage.GetReadStream(indexItem.ItemOffset, indexItem.ItemLength))
        {
            var bytesRead = readStream.Read(buffer, 0, (int) indexItem.ItemLength);
            if (bytesRead != indexItem.ItemLength)
                throw new DataInconsistencyException(
                    $"Tried to read item with {indexItem.ItemOffset} bytes, but only received {bytesRead} bytes. Maybe the data file is currupted.");
        }

        _dataPageFactory.ReleasePage(dataPage.Index);

        return buffer;
    }

    private ReadOnlyMemory<byte> ReadItem(long itemIndex)
    {
        // Get index item for head index
        var indexItem = GetIndexItem(itemIndex);

        return Readitem(indexItem);
    }

    private bool ReadItemIfSmallerThan(long itemIndex, long sizeLimit, [NotNullWhen(true)] out ReadOnlyMemory<byte>? item)
    {
        // Get index item for head index
        var indexItem = GetIndexItem(itemIndex);

        if (indexItem.ItemLength <= sizeLimit)
        {
            item = Readitem(indexItem);
            return true;
        }
        else
        {
            item = null;
            return false;
        }
    }

    private List<ReadOnlyMemory<byte>> ReadItems(long headIndex, int noOfItems)
    {
        return
            Enumerable.Range(0, noOfItems)
                .Select(offset => headIndex + offset)
                .Select(ReadItem)
                .ToList();
    }

    private IReadOnlyList<ReadOnlyMemory<byte>> ReadItemsWithSizeLimit(long headIndex, int noOfItems, long maxSize)
    {
        var data = new List<ReadOnlyMemory<byte>>(noOfItems);

        var firstItem = ReadItem(headIndex);
        data.Add(firstItem);
        var dataSize = firstItem.Length;

        var offset = 1;
        while (offset < noOfItems && dataSize < maxSize)
        {
            var itemSizeLimit = maxSize - dataSize;
            if (ReadItemIfSmallerThan(headIndex + offset, itemSizeLimit, out var item))
            {
                data.Add(item.Value);
                dataSize += item.Value.Length;
            }
            else
                break;

            offset++;
        }

        return data;
    }

    private static void RejectBatch(ItemRange range)
    {
        // This hook is intended for the future.
        // For now, there is nothing to do when rejecting a batch. Just don't commit the read.
    }
}
