using System;
using System.Collections.Generic;
using NUnit.Framework;
using Persistent.Queue.DataObjects;
using Persistent.Queue.Utils;

namespace PersistentQueue.Tests.Utils.IndexItemHelperTests;

[TestFixture]
public class EstimateQueueDataSize
{
    [TestCaseSource(nameof(GetExamples))]
    public long Run(IndexItem headItem, IndexItem tailItem, long dataPageSize)
    {
        return IndexItemHelper.EstimateQueueDataSize(headItem, tailItem, dataPageSize);
    }

    public static IEnumerable<TestCaseData> GetExamples()
    {
        yield return
            new TestCaseData(
                    new IndexItem() {DataPageIndex = 1, ItemOffset = 0, ItemLength = 2},
                    new IndexItem() {DataPageIndex = 1, ItemOffset = 0, ItemLength = 2},
                    100
                )
                .Returns(2)
                .SetName("Same IndexItem");

        yield return
            new TestCaseData(
                    new IndexItem() {DataPageIndex = 1, ItemOffset = 0, ItemLength = 2},
                    new IndexItem() {DataPageIndex = 1, ItemOffset = 10, ItemLength = 2},
                    1024
                )
                .Returns(12L);

        yield return
            new TestCaseData(
                    new IndexItem() {DataPageIndex = 1, ItemOffset = 0, ItemLength = 2},
                    new IndexItem() {DataPageIndex = 2, ItemOffset = 0, ItemLength = 4},
                    1024
                )
                .Returns(1028L);
            
        yield return
            new TestCaseData(
                    new IndexItem() {DataPageIndex = 1, ItemOffset = 1022, ItemLength = 2},
                    new IndexItem() {DataPageIndex = 2, ItemOffset = 0, ItemLength = 4},
                    1024
                )
                .Returns(6L);

        yield return
            new TestCaseData(
                    new IndexItem() {DataPageIndex = 1, ItemOffset = 0, ItemLength = 2},
                    new IndexItem() {DataPageIndex = 11, ItemOffset = 0, ItemLength = 4},
                    100
                )
                .Returns(1004L);

        yield return
            new TestCaseData(
                    new IndexItem() {DataPageIndex = 1, ItemOffset = 50, ItemLength = 2},
                    new IndexItem() {DataPageIndex = 11, ItemOffset = 0, ItemLength = 4},
                    100
                )
                .Returns(954L);

        yield return
            new TestCaseData(
                    new IndexItem() {DataPageIndex = 2, ItemOffset = 50, ItemLength = 2},
                    new IndexItem() {DataPageIndex = 1, ItemOffset = 0, ItemLength = 4},
                    100
                )
                .Returns(0);

        yield return
            new TestCaseData(
                    new IndexItem() {DataPageIndex = 1, ItemOffset = 1, ItemLength = 2},
                    new IndexItem() {DataPageIndex = 1, ItemOffset = 0, ItemLength = 4},
                    100
                )
                .Returns(0);

    }
}