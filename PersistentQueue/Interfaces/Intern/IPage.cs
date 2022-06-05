namespace Persistent.Queue.Interfaces.Intern;

internal interface IPage : IDisposable
{
    long Index { get; }
    void Delete();
    Stream GetReadStream(long position, long length);
    Stream GetWriteStream(long position, long length);
}
