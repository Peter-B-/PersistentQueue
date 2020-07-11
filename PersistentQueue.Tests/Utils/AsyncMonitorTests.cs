using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PersistentQueue.Utils;
using Shouldly;

namespace PersistentQueue.Tests.Utils
{
    [TestFixture]
    public class AsyncMonitorTests
    {
        [Test]
        public void ManyAwaits()
        {
            // Arrange
            var sut = new AsyncMonitor<long>();

            // Act
            var waitingTasks =
                Enumerable.Range(0, 5)
                    .Select(_ => sut.WaitOne())
                    .ToList();


            // Assert
            waitingTasks.ShouldAllBe(t => t.IsCompleted == false);
            sut.Pulse(12);
            waitingTasks.ShouldAllBe(t => t.IsCompleted);
            waitingTasks.ShouldAllBe(t => t.Result == 12);
        }

        [Test]
        public async Task PulsesBefore()
        {
            // Arrange
            var sut = new AsyncMonitor<long>();

            // Act
            sut.Pulse(1);
            sut.Pulse(2);
            sut.Pulse(3);

            var waitTask = sut.WaitOne();
            sut.Pulse(4);
            var number = await waitTask;

            // Assert
            number.ShouldBe(4);
        }

        [Test]
        public void ShouldContinueOnOtherThread()
        {
            // Arrange
            var sut = new AsyncMonitor<long>();

            var pulseThreadId = Thread.CurrentThread.ManagedThreadId;

            // Act
            var waitThreadIdTask =
                sut.WaitOne()
                    .ContinueWith(t => Thread.CurrentThread.ManagedThreadId);

            sut.Pulse(0);

            // Assert
            var waitThreadId = waitThreadIdTask.Result;
            waitThreadId.ShouldNotBe(pulseThreadId);
            waitThreadId.ShouldNotBe(Thread.CurrentThread.ManagedThreadId);
        }
    }
}