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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Result;
using Xunit;

namespace Neo4j.Driver.Tests.ExecutableQuery;

public class ExecutableQueryTests
{
    [Fact]
    public async Task ShouldReturnSimpleList()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);

        autoMock.GetMock<IQueryRowSource<int>>()
            .Setup(x => x.GetRowsAsync(It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .Callback(
                (Action<int> p, CancellationToken _) =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        p(i);
                    }
                });

        var subject = new ExecutableQuery<int, int>(autoMock.GetMock<IQueryRowSource<int>>().Object, i => i);

        var results = new List<int>();
        await subject.GetRowsAsync(i => results.Add(i), CancellationToken.None);

        results.Should().BeEquivalentTo(Enumerable.Range(0, 10));
    }

    [Fact]
    public async Task ShouldReturnFilteredList()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);

        autoMock.GetMock<IQueryRowSource<int>>()
            .Setup(x => x.GetRowsAsync(It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .Callback(
                (Action<int> p, CancellationToken _) =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        p(i);
                    }
                })
            .ReturnsAsync(new ExecutionSummary(null, null));

        var subject = new ExecutableQuery<int, int>(autoMock.GetMock<IQueryRowSource<int>>().Object, i => i);

        var result = await subject
            .WithFilter(i => i < 5)
            .ExecuteAsync();

        result.Result.Should().BeEquivalentTo(Enumerable.Range(0, 5));
    }

    [Fact]
    public async Task ShouldReturnMappedList()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);

        autoMock.GetMock<IQueryRowSource<int>>()
            .Setup(x => x.GetRowsAsync(It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .Callback(
                (Action<int> p, CancellationToken _) =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        p(i);
                    }
                })
            .ReturnsAsync(new ExecutionSummary(null, null));

        var subject = new ExecutableQuery<int, int>(autoMock.GetMock<IQueryRowSource<int>>().Object, i => i);

        var result = await subject
            .WithMap(i => i + 100)
            .ExecuteAsync();

        result.Result.Should().BeEquivalentTo(Enumerable.Range(100, 10));
    }

    [Fact]
    public async Task ShouldReturnCorrectResultSummary()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);

        var query = new Query("fake cypher");
        var summary = new SummaryBuilder(
            query,
            autoMock.GetMock<IServerInfo>().Object).Build(new CursorMetadata(true, true));

        var keys = new[] { "alpha", "bravo", "charlie" };

        autoMock.GetMock<IQueryRowSource<int>>()
            .Setup(x => x.GetRowsAsync(It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .Callback(
                (Action<int> p, CancellationToken _) =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        p(i);
                    }
                })
            .ReturnsAsync(new ExecutionSummary(summary, keys));

        var subject = new ExecutableQuery<int, int>(autoMock.GetMock<IQueryRowSource<int>>().Object, i => i);

        var result = await subject
            .WithMap(i => i + 100)
            .WithMap(i => i + 100)
            .WithFilter(i => i < 5)
            .ExecuteAsync();

        result.Keys.Should().BeSameAs(keys);
        result.Summary.Should().BeSameAs(summary);
    }

    [Fact]
    public async Task ShouldReturnMappedAndFilteredList()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);

        autoMock.GetMock<IQueryRowSource<int>>()
            .Setup(x => x.GetRowsAsync(It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .Callback(
                (Action<int> p, CancellationToken _) =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        p(i);
                    }
                })
            .ReturnsAsync(new ExecutionSummary(null, null));

        var subject = new ExecutableQuery<int, int>(autoMock.GetMock<IQueryRowSource<int>>().Object, i => i);

        var result = await subject
            .WithMap(i => i + 100)
            .WithFilter(i => i < 105)
            .ExecuteAsync();

        result.Result.Should().BeEquivalentTo(Enumerable.Range(100, 5));
    }

    [Fact]
    public async Task ShouldReturnMultiMappedAndFilteredList()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);

        autoMock.GetMock<IQueryRowSource<int>>()
            .Setup(x => x.GetRowsAsync(It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .Callback(
                (Action<int> p, CancellationToken _) =>
                {
                    for (var i = 0; i < 100; i++)
                    {
                        p(i);
                    }
                })
            .ReturnsAsync(new ExecutionSummary(null, null));

        var subject = new ExecutableQuery<int, int>(autoMock.GetMock<IQueryRowSource<int>>().Object, i => i);

        var result = await subject
            .WithMap(i => i * 2) // 0, 2, 4.. 198
            .WithFilter(i => i < 100) // 0, 2, 4.. 98
            .WithMap(i => i / 2) // 0, 1, 2.. 49
            .WithFilter(i => i < 20) // 0, 1, 2.. 19
            .WithMap(i => i * 3) // 0, 3, 6.. 57
            .WithFilter(i => i < 10) // 0, 3, 6, 9
            .ExecuteAsync();

        result.Result.Should().BeEquivalentTo(new[] { 0, 3, 6, 9 });
    }

    [Fact]
    public async Task ShouldReturnReducedValue()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);

        autoMock.GetMock<IQueryRowSource<int>>()
            .Setup(x => x.GetRowsAsync(It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .Callback(
                (Action<int> p, CancellationToken _) =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        p(i);
                    }
                })
            .ReturnsAsync(new ExecutionSummary(null, null));

        var subject = new ExecutableQuery<int, int>(autoMock.GetMock<IQueryRowSource<int>>().Object, i => i);

        var result = await subject
            .WithReduce(() => 0, (x, y) => x + y)
            .ExecuteAsync();

        result.Result.Should().Be(45);
    }

    [Fact]
    public async Task ShouldReturnMappedReducedValue()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);

        autoMock.GetMock<IQueryRowSource<int>>()
            .Setup(x => x.GetRowsAsync(It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .Callback(
                (Action<int> p, CancellationToken _) =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        p(i);
                    }
                })
            .ReturnsAsync(new ExecutionSummary(null, null));

        var subject = new ExecutableQuery<int, int>(autoMock.GetMock<IQueryRowSource<int>>().Object, i => i);

        var result = await subject
            .WithMap(i => i * 10)
            .WithReduce(() => 0, (x, y) => x + y)
            .ExecuteAsync();

        result.Result.Should().Be(450);
    }

    [Fact]
    public async Task ShouldReturnMappedTransformedReducedValue()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);

        autoMock.GetMock<IQueryRowSource<int>>()
            .Setup(x => x.GetRowsAsync(It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .Callback(
                (Action<int> p, CancellationToken _) =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        p(i);
                    }
                })
            .ReturnsAsync(new ExecutionSummary(null, null));

        var subject = new ExecutableQuery<int, int>(autoMock.GetMock<IQueryRowSource<int>>().Object, i => i);

        var result = await subject
            .WithMap(i => i * 10)
            .WithReduce(() => 0, (x, y) => x + y, i => $"<{i * 2}>")
            .ExecuteAsync();

        result.Result.Should().Be("<900>");
    }

    [Fact]
    public async Task ShouldSetConfig()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);
        var config = new QueryConfig();

        var driver = autoMock.GetMock<IDriverRowSource<int>>();
        driver
            .Setup(x => x.GetRowsAsync(It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutionSummary(null, null));

        var subject = new ExecutableQuery<int, int>(driver.Object, i => i);

        await subject
            .WithConfig(config)
            .ExecuteAsync();

        driver.Verify(x => x.SetConfig(config));
    }

    [Fact]
    public async Task ShouldSetParametersWithObject()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);
        var parameters = new { Shaken = true, Stirred = false };

        var driver = autoMock.GetMock<IDriverRowSource<int>>();
        driver
            .Setup(x => x.GetRowsAsync(It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutionSummary(null, null));

        var subject = new ExecutableQuery<int, int>(driver.Object, i => i);

        await subject
            .WithParameters(parameters)
            .ExecuteAsync();

        driver.Verify(x => x.SetParameters(parameters));
    }

    [Fact]
    public async Task ShouldSetParametersWithDictionary()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);
        var parameters = new Dictionary<string, object>
        {
            ["shaken"] = true,
            ["stirred"] = false
        };

        var driver = autoMock.GetMock<IDriverRowSource<int>>();
        driver
            .Setup(x => x.GetRowsAsync(It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExecutionSummary(null, null));

        var subject = new ExecutableQuery<int, int>(driver.Object, i => i);

        await subject
            .WithParameters(parameters)
            .ExecuteAsync();

        driver.Verify(x => x.SetParameters(parameters));
    }

    [Fact]
    public async Task ShouldUseStreamProcessor()
    {
        var autoMock = new AutoMocker(MockBehavior.Loose);

        async IAsyncEnumerable<int> GetInts(int start, int count)
        {
            foreach (var i in Enumerable.Range(start, count))
            {
                await Task.Yield();
                yield return i;
            }
        }

        var driverMock = autoMock.GetMock<IDriverRowSource<int>>();
        driverMock
            .Setup(
                x => x.ProcessStreamAsync(
                    It.IsAny<Func<IAsyncEnumerable<int>, Task<int>>>(),
                    It.IsAny<CancellationToken>()))
            .Returns<Func<IAsyncEnumerable<int>, Task<int>>, CancellationToken>(
                (p, _) =>
                    Task.FromResult(
                        new EagerResult<int>(
                            p(GetInts(0, 10)).GetAwaiter().GetResult(),
                            null,
                            null)));

        var subject = new ExecutableQuery<int, int>(driverMock.Object, i => i);

        var rnd = Random.Shared.Next();

        var queryExecution = await subject.WithStreamProcessor(
                async stream =>
                {
                    var result = rnd;

                    await foreach (var i in stream)
                    {
                        result += i;
                    }

                    return result;
                })
            .ExecuteAsync();

        queryExecution.Result.Should().Be(45 + rnd);
    }
}
