using System.Globalization;
using Persistent.Queue.Cache;

namespace Persistent.Queue.Utils;

internal sealed class PageFactory : IDisposable
{
    private static readonly string PageFileName = "page";
    private static readonly string PageFileSuffix = ".dat";
    private readonly Cache<long, Page> _pageCache;
    private readonly string _pageDir;
    private readonly long _pageSize;
    private bool _disposed;

    public PageFactory(long pageSize, string pageDirectory, TimeSpan? cacheTtl = null)
    {
        _pageSize = pageSize;
        _pageDir = pageDirectory;

        if (!Directory.Exists(_pageDir))
            Directory.CreateDirectory(_pageDir);

        // A simple cache using the page filename as key.
        _pageCache = new Cache<long, Page>(cacheTtl ?? TimeSpan.FromSeconds(10));
    }

    public void Dispose()
    {
        if (_disposed) return;

        _pageCache.Dispose();

        _disposed = true;
    }

    public void DeletePage(long index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_pageCache.TryRemoveValue(index, out var page))
            page.Delete();
        else
            // If not found in cache, delete the file directly.
            Page.DeleteFile(GetFilePath(index));
    }

    public Page GetPage(long index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _pageCache.GetOrCreate(index, () => new Page(GetFilePath(index), _pageSize, index));
    }

    public void ReleasePage(long index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _pageCache.Release(index);
    }

    private string GetFilePath(long index)
    {
        return Path.Combine(_pageDir, string.Format(CultureInfo.InvariantCulture, "{0}-{1}{2}", PageFileName, index, PageFileSuffix));
    }
}
