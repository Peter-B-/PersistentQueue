using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Persistent.Queue.Utils;
using Shouldly;

namespace PersistentQueue.Tests.Utils;

[TestFixture]
public class QueueStateMonitorTests
{
    [Test]
    public void ManyAwaits()
    {
        // Arrange
        var sut = QueueStateMonitor.Initialize(0);

        // Act
        var waitingTasks =
            Enumerable.Range(0, 5)
                .Select(_ => sut.GetCurrent().NextUpdate)
                .ToList();


        // Assert
        waitingTasks.ShouldAllBe(t => t.IsCompleted == false);
        sut.Update(12);
        waitingTasks.ShouldAllBe(t => t.IsCompleted);
        waitingTasks.ShouldAllBe(t => t.Result.TailIndex == 12);
    }

    [Test]
    public async Task PulsesBefore()
    {
        // Arrange
        var sut = QueueStateMonitor.Initialize(0);

        // Act
        sut.Update(1);
        sut.Update(2);
        sut.Update(3);

        var state = sut.GetCurrent();
        state.TailIndex.ShouldBe(3);
        sut.Update(4);
        var update = await state.NextUpdate;

        // Assert
        update.TailIndex.ShouldBe(4);
    }

    [Test]
    public void ShouldContinueOnOtherThread()
    {
        // Arrange
        var sut = QueueStateMonitor.Initialize(0);
        ;

        var pulseThreadId = Environment.CurrentManagedThreadId;

        // Act
        var waitThreadIdTask =
            sut.GetCurrent().NextUpdate
                .ContinueWith(t => Environment.CurrentManagedThreadId);

        sut.Update(0);

        // Assert
        var waitThreadId = waitThreadIdTask.Result;
        waitThreadId.ShouldNotBe(pulseThreadId);
        waitThreadId.ShouldNotBe(Environment.CurrentManagedThreadId);
    }
}
