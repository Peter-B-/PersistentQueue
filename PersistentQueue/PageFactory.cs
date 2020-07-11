using System;
using System.IO;
using PersistentQueue.Cache;

namespace PersistentQueue
{
    internal class PageFactory : IPageFactory
    {
        private static readonly string PageFileName = "page";
        private static readonly string PageFileSuffix = ".dat";
        private readonly string PageDir;
        private readonly long PageSize;
        private readonly Cache<long, IPage> _pageCache;
        private bool disposed;

        public PageFactory(long pageSize, string pageDirectory)
        {
            PageSize = pageSize;
            PageDir = pageDirectory;

            if (!Directory.Exists(PageDir))
                Directory.CreateDirectory(PageDir);

            // A simple cache using the page filename as key.
            _pageCache = new Cache<long, IPage>(10000);
        }

        public IPage GetPage(long index)
        {
            IPage page;

            if (!_pageCache.TryGetValue(index, out page))
            {
                page = _pageCache[index] = new Page(GetFilePath(index), PageSize, index);
            }

            return page;
        }

        public void ReleasePage(long index)
        {
            _pageCache.Release(index);
        }

        public void DeletePage(long index)
        {
            IPage page;

            // Lookup page in _pageCache.
            if (_pageCache.TryGetValue(index, out page))
            {
                // delete and remove from cache
                page.Delete();
                _pageCache.Remove(index);
            }
            else
            {
                // If not found in cache, delete the file directly.
                Page.DeleteFile(GetFilePath(index));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private string GetFilePath(long index)
        {
            return Path.Combine(PageDir, string.Format("{0}-{1}{2}", PageFileName, index, PageFileSuffix));
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (_pageCache != null)
                    _pageCache.RemoveAll();
            }

            disposed = true;
        }

        ~PageFactory()
        {
            Dispose(false);
        }
    }
}