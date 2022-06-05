using Persistent.Queue.DataObjects;

namespace Persistent.Queue.Utils;

public static class IndexItemHelper
{
    public static long EstimateQueueDataSize(IndexItem headItem, IndexItem tailItem, long dataPageSize)
    {
        var filledPages = tailItem.DataPageIndex - headItem.DataPageIndex;
        if (filledPages < 0)
            // Invalid: head behind tail
            return 0L;

        if (filledPages == 0)
        {
            if (tailItem.ItemOffset < headItem.ItemOffset)
                // Invalid: head behind tail
                return 0L;

            // head and tail are on same page
            return tailItem.ItemOffset - headItem.ItemOffset + tailItem.ItemLength;
        }

        return
            // rest of head page
            dataPageSize - headItem.ItemOffset +
            // pages between head and tail
            (filledPages - 1) * dataPageSize +
            // top of tail page
            tailItem.ItemOffset + tailItem.ItemLength;
    }
}
