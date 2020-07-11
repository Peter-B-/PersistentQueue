using System;
using System.IO;

namespace PersistentQueue
{
    internal interface IPage : IDisposable
    {
        long Index { get; }
        Stream GetWriteStream(long position, long length);
        Stream GetReadStream(long position, long length);
        void Delete();
        void DeleteFile(string filePath);
    }
}