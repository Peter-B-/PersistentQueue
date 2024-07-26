namespace Persistent.Queue.DataObjects;

internal sealed class MetaData
{
    public long HeadIndex { get; set; }
    public long TailIndex { get; set; }

    public static MetaData ReadFromStream(Stream s)
    {
        using var br = new BinaryReader(s);
        var ret = new MetaData
        {
            HeadIndex = br.ReadInt64(),
            TailIndex = br.ReadInt64()
        };

        return ret;
    }

    public static long Size()
    {
        return 2 * sizeof(long);
    }

    public void WriteToStream(Stream s)
    {
        using var bw = new BinaryWriter(s);

        bw.Write(HeadIndex);
        bw.Write(TailIndex);
    }
}
