using System;

namespace Persistent.Queue.Interfaces.Intern
{
    internal interface IPageFactory : IDisposable
    {
        IPage GetPage(long index);
        void ReleasePage(long index);
        void DeletePage(long index);
    }
}