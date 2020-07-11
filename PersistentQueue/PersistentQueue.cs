﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace PersistentQueue
{
    public class PersistentQueue : IDisposable
    {
        // Folders
        readonly string QueuePath;
        static readonly string MetaPageFolder = "meta";
        static readonly string IndexPageFolder = "index";
        static readonly string DataPageFolder = "data";

        // MetaData
        MetaData _metaData;
        readonly long MetaDataItemSize;
        IPageFactory _metaPageFactory;

        // Tail info
        long _tailDataPageIndex;
        long _tailDataItemOffset;

        // Head info
        long _headDataPageIndex;
        long _headIndexPageIndex;

        // Index pages
        static readonly long IndexItemsPerPage = 50000;
        readonly long IndexItemSize;
        readonly long IndexPageSize;
        IPageFactory _indexPageFactory;

        // Data pages
        readonly long DataPageSize;
        static readonly long DefaultDataPageSize = 128 * 1024 * 1024;
        static readonly long MinimumDataPageSize = 32 * 1024 * 1024;
        IPageFactory _dataPageFactory;

        Object _lockObject = new Object();

        public PersistentQueue(string queuePath) : this(queuePath, DefaultDataPageSize) { }

        public PersistentQueue(string queuePath, long pageSize)
        {
            QueuePath = queuePath;
            DataPageSize = pageSize;

            MetaDataItemSize = MetaData.Size();
            IndexItemSize = IndexItem.Size();
            IndexPageSize = IndexItemSize * IndexItemsPerPage;

            Init();
        }

        void Init()
        {
            // Init page factories
            _metaPageFactory = new PageFactory(MetaDataItemSize, Path.Combine(QueuePath, MetaPageFolder)); // Page size = item size => only 1 item possible.
            _indexPageFactory = new PageFactory(IndexPageSize, Path.Combine(QueuePath, IndexPageFolder));
            _dataPageFactory = new PageFactory(DataPageSize, Path.Combine(QueuePath, DataPageFolder));

            InitializeMetaData();
        }

        void InitializeMetaData()
        {
            var metaPage = _metaPageFactory.GetPage(0);
            using (var readStream = metaPage.GetReadStream(0, MetaDataItemSize))
            {
                _metaData = MetaData.ReadFromStream(readStream);
            }

            // Update local data pointers from previously persisted index item
            var prevTailIndex = GetPreviousIndex(_metaData.TailIndex);
            var prevTailIndexItem = GetIndexItem(prevTailIndex);
            _tailDataPageIndex = prevTailIndexItem.DataPageIndex;
            _tailDataItemOffset = prevTailIndexItem.ItemOffset + prevTailIndexItem.ItemLength;

            var prevHeadIndex = GetPreviousIndex(_metaData.HeadIndex);
            var prevHeadIndexItem = GetIndexItem(prevHeadIndex);
            _headDataPageIndex = prevHeadIndexItem.DataPageIndex;
            _headIndexPageIndex = GetIndexPageIndex(GetPreviousIndex(_metaData.HeadIndex));
        }

        long GetIndexPageIndex(long index)
        {
            return index / IndexItemsPerPage;
        }
        long GetIndexItemOffset(long index)
        {
            return (index % IndexItemsPerPage) * IndexItemSize;
        }

        long GetPreviousIndex(long index)
        {
            // TODO: Handle wrap situations => index == long.MaxValue
            if (index > 0)
                return index - 1;
            
            return index;
        }

        IndexItem GetIndexItem(long index)
        {
            IndexItem indexItem;

            var indexPage = _indexPageFactory.GetPage(GetIndexPageIndex(index));
            using (var stream = indexPage.GetReadStream(GetIndexItemOffset(index), IndexItemSize))
            {
                indexItem = IndexItem.ReadFromStream(stream);
            }
            _indexPageFactory.ReleasePage(indexPage.Index);
            
            return indexItem;
        }

        void PersistMetaData()
        {
            var metaPage = _metaPageFactory.GetPage(0);
            using (var writeStream = metaPage.GetWriteStream(0, MetaDataItemSize))
            {
                _metaData.WriteToStream(writeStream);
            }
        }

        public void Enqueue(Stream itemData)
        {
            lock (_lockObject)
            {
                // Throw or silently return if itemData is null?
                if (itemData == null)
                    return;

                if (itemData.Length > DataPageSize)
                    throw new ArgumentOutOfRangeException("Item data length is greater than queue data page size");

                if (_tailDataItemOffset + itemData.Length > DataPageSize)       // Not enough space in current page
                {
                    if (_tailDataPageIndex == long.MaxValue)
                        _tailDataPageIndex = 0;
                    else
                        _tailDataPageIndex++;

                    _tailDataItemOffset = 0;
                }

                // Get data page
                var dataPage = _dataPageFactory.GetPage(_tailDataPageIndex);

                // Get write stream
                using (var writeStream = dataPage.GetWriteStream(_tailDataItemOffset, itemData.Length))
                {
                    // Write data to write stream
                    itemData.CopyTo(writeStream, 4 * 1024);
                }

                // Release our reference to the data page
                _dataPageFactory.ReleasePage(_tailDataPageIndex);

                // Udate index
                // Get index page
                var indexPage = _indexPageFactory.GetPage(GetIndexPageIndex(_metaData.TailIndex));

                // Get write stream
                using (var writeStream = indexPage.GetWriteStream(GetIndexItemOffset(_metaData.TailIndex), IndexItemSize))
                {
                    var indexItem = new IndexItem
                    {
                        DataPageIndex = _tailDataPageIndex,
                        ItemOffset = _tailDataItemOffset,
                        ItemLength = itemData.Length
                    };
                    indexItem.WriteToStream(writeStream);
                }

                _indexPageFactory.ReleasePage(GetIndexPageIndex(_metaData.TailIndex));

                // Advance
                _tailDataItemOffset += itemData.Length;

                // Update meta data
                if (_metaData.TailIndex == long.MaxValue)
                    _metaData.TailIndex = 0;
                else
                    _metaData.TailIndex++;
                PersistMetaData();
            }
        }

        public Stream Dequeue()
        {
            lock (_lockObject)
            {
                if (_metaData.HeadIndex == _metaData.TailIndex)     // Head cought up with tail. Queue is empty.
                    return null;                                    // return null or Stream.Null?

                // Delete previous index page if we are moving along to the next
                if (GetIndexPageIndex(_metaData.HeadIndex) != _headIndexPageIndex)
                {
                    _indexPageFactory.DeletePage(_headIndexPageIndex);
                    _headIndexPageIndex = GetIndexPageIndex(_metaData.HeadIndex);
                }

                // Get index item for head index
                var indexItem = GetIndexItem(_metaData.HeadIndex);

                // Delete previous data page if we are moving along to the next
                if (indexItem.DataPageIndex != _headDataPageIndex)
                {
                    _dataPageFactory.DeletePage(_headDataPageIndex);
                    _headDataPageIndex = indexItem.DataPageIndex;
                }

                // Get data page
                var dataPage = _dataPageFactory.GetPage(indexItem.DataPageIndex);

                // Get read stream
                MemoryStream memoryStream = new MemoryStream();
                using (var readStream = dataPage.GetReadStream(indexItem.ItemOffset, indexItem.ItemLength))
                {
                    readStream.CopyTo(memoryStream, 4 * 1024);
                    memoryStream.Position = 0;
                }

                _dataPageFactory.ReleasePage(dataPage.Index);

                // Update meta data
                if (_metaData.HeadIndex == long.MaxValue)
                    _metaData.HeadIndex = 0;
                else
                    _metaData.HeadIndex++;

                PersistMetaData();

                return memoryStream;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _metaPageFactory?.Dispose();
                _indexPageFactory?.Dispose();
                _dataPageFactory?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
