// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;
using Record = Neo4j.Driver.Internal.Result.Record;

namespace Neo4j.Driver.Tests;

internal class ListBasedRecordCursor : IInternalResultCursor
{
    private readonly string[] _keys;
    private readonly Func<IEnumerable<IRecord>> _recordsFunc;
    private readonly Func<IResultSummary> _summaryFunc;
    private IEnumerator<IRecord> _enum;
    private IRecord _peeked;

    private IResultSummary _summary;

    public ListBasedRecordCursor(
        IEnumerable<string> keys,
        Func<IEnumerable<IRecord>> recordsFunc,
        Func<IResultSummary> summaryFunc = null)
    {
        _keys = keys.ToArray();
        _recordsFunc = recordsFunc;
        _summaryFunc = summaryFunc;
    }

    public Task<string[]> KeysAsync()
    {
        return Task.FromResult(_keys);
    }

    public Task<IRecord> PeekAsync()
    {
        if (_peeked == null)
        {
            if (_enum == null)
            {
                _enum = _recordsFunc().GetEnumerator();
            }

            if (_enum.MoveNext())
            {
                _peeked = _enum.Current;
            }
        }

        return Task.FromResult(_peeked);
    }

    public async Task<IResultSummary> ConsumeAsync()
    {
        while (await FetchAsync())
        {
        }

        return await GetSummaryAsync();
    }

    public Task<bool> FetchAsync()
    {
        if (_enum == null)
        {
            _enum = _recordsFunc().GetEnumerator();
        }

        if (_peeked != null)
        {
            Current = _peeked;
            _peeked = null;
            return Task.FromResult(true);
        }

        var hasNext = _enum.MoveNext();
        Current = hasNext ? _enum.Current : null;
        return Task.FromResult(hasNext);
    }

    public IRecord Current { get; private set; }

    public bool IsOpen => true;

    public void Cancel()
    {
    }

    private Task<IResultSummary> GetSummaryAsync()
    {
        if (_summary == null && _summaryFunc != null)
        {
            _summary = _summaryFunc();
        }

        return Task.FromResult(_summary);
    }
}

internal static class RecordCreator
{
    public static string[] CreateKeys(int keySize = 1)
    {
        return Enumerable.Range(0, keySize).Select(i => $"key{i}").ToArray();
    }

    public static IList<IRecord> CreateRecords(int recordSize, int keySize = 1)
    {
        var keys = CreateKeys(keySize);
        return CreateRecords(recordSize, keys);
    }

    public static IList<IRecord> CreateRecords(int recordSize, string[] keys)
    {
        return Enumerable.Range(0, recordSize)
            .Select(i => new Record(keys, keys.Select(k => $"record{i}:{k}").Cast<object>().ToArray()))
            .Cast<IRecord>()
            .ToList();
    }
}

public class RecordSetTests
{
    public class RecordMethod
    {
        [Fact]
        public async Task ShouldReturnRecordsInOrder()
        {
            var records = RecordCreator.CreateRecords(5);
            var cursor = new ListBasedRecordCursor(new[] { "key1" }, () => records);

            var i = 0;
            while (await cursor.FetchAsync())
            {
                cursor.Current[0].As<string>().Should().Be($"record{i++}:key0");
            }

            i.Should().Be(5);
        }

        [Fact]
        public async Task ShouldReturnRecordsAddedLatter()
        {
            var keys = RecordCreator.CreateKeys();
            var records = RecordCreator.CreateRecords(5, keys);
            var cursor = new ListBasedRecordCursor(keys, () => records);

            // I add a new record after RecordSet is created
            var newRecord = new Record(keys, new object[] { "record5:key0" });
            records.Add(newRecord);

            var i = 0;
            while (await cursor.FetchAsync())
            {
                cursor.Current[0].As<string>().Should().Be($"record{i++}:key0");
            }

            i.Should().Be(6);
        }
    }

    public class PeekMethod
    {
        [Fact]
        public async Task ShouldReturnNextRecordWithoutMovingCurrentRecord()
        {
            var records = RecordCreator.CreateRecords(5);
            var cursor = new ListBasedRecordCursor(new[] { "key1" }, () => records);

            var record = await cursor.PeekAsync();
            record.Should().NotBeNull();
            record[0].As<string>().Should().Be("record0:key0");

            // not moving further no matter how many times are called
            record = await cursor.PeekAsync();
            record.Should().NotBeNull();
            record[0].As<string>().Should().Be("record0:key0");
        }

        [Fact]
        public async Task ShouldReturnNextRecordAfterNextWithoutMovingCurrentRecord()
        {
            var records = RecordCreator.CreateRecords(5);
            var cursor = new ListBasedRecordCursor(new[] { "key0" }, () => records);

            await cursor.FetchAsync();

            var record = await cursor.PeekAsync();
            record.Should().NotBeNull();
            record[0].As<string>().Should().Be("record1:key0");

            // not moving further no matter how many times are called
            record = await cursor.PeekAsync();
            record.Should().NotBeNull();
            record[0].As<string>().Should().Be("record1:key0");
        }

        [Fact]
        public async Task ShouldReturnNullIfAtEnd()
        {
            var records = RecordCreator.CreateRecords(5);
            var cursor = new ListBasedRecordCursor(new[] { "key0" }, () => records);

            // [0, 1, 2, 3]
            await cursor.FetchAsync();
            await cursor.FetchAsync();
            await cursor.FetchAsync();
            await cursor.FetchAsync();

            var record = await cursor.PeekAsync();
            record.Should().NotBeNull();
            record[0].As<string>().Should().Be("record4:key0");

            var moveNext = await cursor.FetchAsync();
            moveNext.Should().BeTrue();

            record.Should().NotBeNull();
            record[0].As<string>().Should().Be("record4:key0");

            record = await cursor.PeekAsync();
            record.Should().BeNull();
        }

        [Fact]
        public async Task ShouldBeTheSameWithEnumeratorMoveNextCurrent()
        {
            var records = RecordCreator.CreateRecords(2);
            var cursor = new ListBasedRecordCursor(new[] { "key0" }, () => records);

            IRecord record;
            bool hasNext;
            for (var i = 0; i < 2; i++)
            {
                record = await cursor.PeekAsync();
                record.Should().NotBeNull();
                record[0].As<string>().Should().Be($"record{i}:key0");

                hasNext = await cursor.FetchAsync();
                hasNext.Should().BeTrue();

                // peeked record = current
                cursor.Current[0].As<string>().Should().Be($"record{i}:key0");
            }

            record = await cursor.PeekAsync();
            record.Should().BeNull();

            hasNext = await cursor.FetchAsync();
            hasNext.Should().BeFalse();
        }
    }
}
