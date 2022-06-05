using System.IO;

namespace Persistent.Queue.DataObjects;

public class IndexItem
{
    public long DataPageIndex { get; set; }
    public long ItemOffset { get; set; }
    public long ItemLength { get; set; }

    public void WriteToStream(Stream s)
    {
        using var bw = new BinaryWriter(s);
        bw.Write(DataPageIndex);
        bw.Write(ItemOffset);
        bw.Write(ItemLength);
    }

    public static IndexItem ReadFromStream(Stream s)
    {
        using var br = new BinaryReader(s);
        var ret = new IndexItem
        {
            DataPageIndex = br.ReadInt64(),
            ItemOffset = br.ReadInt64(),
            ItemLength = br.ReadInt64()
        };

        return ret;
    }

    public static long Size()
    {
        return 3 * sizeof(long);
    }

    public override string ToString()
    {
        return $"Page {DataPageIndex}, @ {ItemOffset}, {ItemLength} bytes";
    }
}