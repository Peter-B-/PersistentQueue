using System.IO.MemoryMappedFiles;

namespace Persistent.Queue.Utils;

internal sealed class Page(string pageFile, long pageSize, long pageIndex) : IDisposable
{
    private readonly MemoryMappedFile _mmf = MemoryMappedFile.CreateFromFile(pageFile, FileMode.OpenOrCreate, null, pageSize,
                                                                             MemoryMappedFileAccess.ReadWrite);

    private bool _disposed;

    public long Index { get; } = pageIndex;

    public void Dispose()
    {
        if (_disposed) return;

        _mmf.Dispose();

        _disposed = true;
    }

    public void Delete()
    {
        Dispose();

        DeleteFile(pageFile);
    }

    public static void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public Stream GetReadStream(long position, long length)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _mmf.CreateViewStream(position, length, MemoryMappedFileAccess.Read);
    }

    public Stream GetWriteStream(long position, long length)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _mmf.CreateViewStream(position, length, MemoryMappedFileAccess.Write);
    }
}
