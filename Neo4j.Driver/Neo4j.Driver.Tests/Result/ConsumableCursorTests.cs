// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class ConsumedException
    {
        public static void ThrowsResultConsumedException(Func<object> func)
        {
            var ex = Record.Exception(func);
            ex.Should().NotBeNull();
            ex.Should().BeOfType<ResultConsumedException>();
        }

        public static async Task ThrowsResultConsumedException<T>(Func<Task<T>> func)
        {
            var ex = await Record.ExceptionAsync(func);
            ex.Should().NotBeNull();
            ex.Should().BeOfType<ResultConsumedException>();
        }
    }

    public class ConsumableCursorTests
    {
        [Fact]
        public async void ShouldErrorWhenAccessRecordsAfterConsume()
        {
            var result = ResultCursorCreator.CreateResultCursor(1, 3);
            await result.ConsumeAsync();

            await ConsumedException.ThrowsResultConsumedException(async () => await result.FetchAsync());
            await ConsumedException.ThrowsResultConsumedException(async () => await result.PeekAsync());
            ConsumedException.ThrowsResultConsumedException(() => result.Current);
        }

        [Fact]
        public async void ShouldErrorWhenAccessRecordsViaExtensionMethodsAfterConsume()
        {
            var result = ResultCursorCreator.CreateResultCursor(1, 3);
            await result.ConsumeAsync();

            await ConsumedException.ThrowsResultConsumedException(async () => await result.SingleAsync());
            await ConsumedException.ThrowsResultConsumedException(async () => await result.ToListAsync());
            await ConsumedException.ThrowsResultConsumedException(async () => await result.ForEachAsync(r => {}));
        }

        [Fact]
        public async void ShouldAllowKeysAndConsumeAfterConsume()
        {
            var result = ResultCursorCreator.CreateResultCursor(1, 3);
            var summary0 = await result.ConsumeAsync();

            var keys1 = await result.KeysAsync();
            var summary1 = await result.ConsumeAsync();

            var keys2 = await result.KeysAsync();
            var summary2 = await result.ConsumeAsync();

            summary1.Should().Be(summary0);
            summary2.Should().Be(summary0);

            keys1.Should().BeEquivalentTo(keys2);
        }

        private static class ResultCursorCreator
        {
            public static IInternalResultCursor CreateResultCursor(
                int keySize,
                int recordSize = 1,
                Func<Task<IResultSummary>> getSummaryFunc = null,
                CancellationTokenSource cancellationTokenSource = null)
            {
                var cursor = ResultCursorTests.ResultCursorCreator.CreateResultCursor(
                    keySize,
                    recordSize,
                    getSummaryFunc,
                    cancellationTokenSource);

                return new ConsumableResultCursor(cursor);
            }
        }
    }
}
