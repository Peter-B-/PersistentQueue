using System.IO.MemoryMappedFiles;
using Persistent.Queue.Interfaces.Intern;

namespace Persistent.Queue.Utils;

internal class Page(string pageFile, long pageSize, long pageIndex) : IPage
{
    private readonly MemoryMappedFile _mmf = MemoryMappedFile.CreateFromFile(pageFile, FileMode.OpenOrCreate, null, pageSize,
                                                                             MemoryMappedFileAccess.ReadWrite);

    private bool _disposed;

    public long Index { get; } = pageIndex;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Delete()
    {
        Dispose(true);
        DeleteFile(pageFile);
    }

    public Stream GetReadStream(long position, long length)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Page));

        return _mmf.CreateViewStream(position, length, MemoryMappedFileAccess.Read);
    }

    public Stream GetWriteStream(long position, long length)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Page));

        return _mmf.CreateViewStream(position, length, MemoryMappedFileAccess.Write);
    }

    public static void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    protected void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            _mmf?.Dispose();

        _disposed = true;
    }

    ~Page()
    {
        Dispose(false);
    }
}
