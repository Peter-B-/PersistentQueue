using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace PersistentQueue
{
    internal class Page : IPage
    {
        private readonly MemoryMappedFile _mmf;
        private readonly string _pageFile;
        private bool disposed;

        public Page(string pageFile, long pageSize, long pageIndex)
        {
            _pageFile = pageFile;
            Index = pageIndex;
            _mmf = MemoryMappedFile.CreateFromFile(pageFile, FileMode.OpenOrCreate, null, pageSize,
                MemoryMappedFileAccess.ReadWrite);
        }

        public long Index { get; }

        public Stream GetReadStream(long position, long length)
        {
            return _mmf.CreateViewStream(position, length, MemoryMappedFileAccess.Read);
        }

        public Stream GetWriteStream(long position, long length)
        {
            return _mmf.CreateViewStream(position, length, MemoryMappedFileAccess.Write);
        }

        public void Delete()
        {
            Dispose();
            DeleteFile(_pageFile);
        }

        void IPage.DeleteFile(string filePath)
        {
            DeleteFile(filePath);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                if (_mmf != null)
                    _mmf.Dispose();
            disposed = true;
        }

        ~Page()
        {
            Dispose(false);
        }
    }
}