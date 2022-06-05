namespace Persistent.Queue.Interfaces.Intern;

internal interface IPageFactory : IDisposable
{
    void DeletePage(long index);
    IPage GetPage(long index);
    void ReleasePage(long index);
}
