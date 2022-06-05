using System;
using System.IO;
using Persistent.Queue.Cache;
using Persistent.Queue.Interfaces.Intern;

namespace Persistent.Queue.Utils;

internal class PageFactory : IPageFactory
{
    private static readonly string PageFileName = "page";
    private static readonly string PageFileSuffix = ".dat";
    private readonly ICache<long, IPage> _pageCache;
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
        _pageCache = new Cache<long, IPage>(cacheTtl ?? TimeSpan.FromSeconds(10));
    }

    public IPage GetPage(long index)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PageFactory));

        return _pageCache.GetOrCreate(index, () => new Page(GetFilePath(index), _pageSize, index));
    }

    public void ReleasePage(long index)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PageFactory));

        _pageCache.Release(index);
    }

    public void DeletePage(long index)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PageFactory));

        if (_pageCache.TryRemoveValue(index, out var page))
            page.Delete();
        else
            // If not found in cache, delete the file directly.
            Page.DeleteFile(GetFilePath(index));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private string GetFilePath(long index)
    {
        return Path.Combine(_pageDir, string.Format("{0}-{1}{2}", PageFileName, index, PageFileSuffix));
    }

    protected void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing) _pageCache?.Dispose();

        _disposed = true;
    }
}