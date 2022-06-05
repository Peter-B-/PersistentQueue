using JetBrains.Annotations;

namespace Persistent.Queue;

[NoReorder]
public class PersistentQueueConfiguration
{
    public PersistentQueueConfiguration(string queuePath, long? dataPageSize = null)
    {
        QueuePath = queuePath;
        DataPageSize = dataPageSize ?? DefaultDataPageSize;
    }

    public string QueuePath { get; }
    public string MetaPageFolder { get; set; } = "meta";
    public string IndexPageFolder { get; set; } = "index";
    public string DataPageFolder { get; set; } = "data";

    // Index pages
    public long IndexItemsPerPage { get; set; } = 50000;
    public long DataPageSize { get; set; }

    public static long DefaultDataPageSize { get; } = 128 * 1024 * 1024;

    /// <summary>
    ///     Minimum number of items returned in a single dequeue operation. <br />
    ///     The dequeue operation will not complete until <c>MinDequeueBatchSize</c> items can be returned.
    /// </summary>
    public int MinDequeueBatchSize { get; set; } = 1;

    /// <summary>
    ///     <para>
    ///         The maximum number of items returned in a single dequeue operation.
    ///     </para>
    ///     <para>
    ///         The dequeue operation will collect items until
    ///         <list type="bullet">
    ///             <item>There are currently no more items in the PersistentQueue.</item>
    ///             <item>There are <c>MaxDequeueBatchSize</c> items in the current batch.</item>
    ///             <item>There combined size of the items would exceed <c>MaxDequeueBatchSizeInBytes</c> items, if the next item is added.</item>
    ///         </list>
    ///     </para>
    /// </summary>
    public int MaxDequeueBatchSize { get; set; } = 100;

    /// <summary>
    ///     <para>
    ///         Size limit for the number of items returned in a single batch.
    ///     </para>
    ///     <para>
    ///         Warning: If <c>ThrowExceptionWhenItemExceedingMaxDequeueBatchSizeIsEnqueued</c> is false, a batch can exceed this size, if a large item was
    ///         enqueued.
    ///     </para>
    /// </summary>
    public long? MaxDequeueBatchSizeInBytes { get; set; }

    /// <summary>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>true</term>
    ///             <description>An InvalidOperationException is thrown, if an item exceeding <c>MaxDequeueBatchSizeInBytes</c> is enqueued.</description>
    ///         </listheader>
    ///         <item>
    ///             <term>false</term>
    ///             <description>Items exceeding <c>MaxDequeueBatchSizeInBytes</c> can be enqueued.</description>
    ///         </item>
    ///     </list>
    /// </summary>
    public bool ThrowExceptionWhenItemExceedingMaxDequeueBatchSizeIsEnqueued { get; set; } = true;

    public string GetMetaPath() => Path.Combine(QueuePath, MetaPageFolder);
    public string GetIndexPath() => Path.Combine(QueuePath, IndexPageFolder);
    public string GetDataPath() => Path.Combine(QueuePath, DataPageFolder);
}
