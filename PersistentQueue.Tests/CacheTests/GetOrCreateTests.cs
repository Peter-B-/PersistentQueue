using System;
using NUnit.Framework;
using Persistent.Queue.Cache;
using Shouldly;

namespace PersistentQueue.Tests.CacheTests
{
    [TestFixture]
    public class GetOrCreateTests
    {
        [Test]
        public void SameItemIsOnlyCreatedOnce()
        {
            using var sut = new Cache<int, TestItem>(TimeSpan.FromSeconds(10));

            var first = sut.GetOrCreate(1, () => new TestItem(1));
            var second = sut.GetOrCreate(1, () => throw new Exception("Second factory should not be invoked"));

            first.ShouldBe(second);
            second.Id.ShouldBe(1);
        }

        [Test]
        public void OtherKey_NewItemIsCreated()
        {
            using var sut = new Cache<int, TestItem>(TimeSpan.FromSeconds(10));

            var first = sut.GetOrCreate(1, () => new TestItem(1));
            var second = sut.GetOrCreate(2, () => new TestItem(2));

            first.Id.ShouldBe(1);
            second.Id.ShouldBe(2);
        }
    }

    public class TestItem
    {
        public int Id { get; }

        public TestItem(int id)
        {
            Id = id;
        }
    }
}
