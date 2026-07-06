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
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Tests.Internal.Core;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

internal class SimpleRetryLogic : IAsyncRetryLogic
{
    private Func<Func<Task<object>>, Task<object>> _funcRunner;

    public SimpleRetryLogic(Func<Func<Task<object>>, Task<object>> funcRunner)
    {
        _funcRunner = funcRunner;
    }

    public async Task<T> RetryAsync<T>(Func<Task<T>> runTxAsyncFunc)
    {
        var obj = await _funcRunner(async () => await runTxAsyncFunc());
        return (T)obj;
    }
}

internal class QueryApiCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize(new AutoMoqCustomization { ConfigureMembers = true });
        fixture.Customize(new LoggingCustomization());
        fixture.Register(() => SessionConfig.Builder.Build());
        fixture.Register<IBookmarkTracker>(() => new BookmarkTracker(SessionConfig.Builder.Build()));
    }
}

internal static class FixtureExtensions
{
    public static IFixture AddPassThroughRetryLogic(this IFixture fixture)
    {
        fixture.Register<IAsyncRetryLogic>(() => new SimpleRetryLogic(fn => fn()));
        return fixture;
    }

    public static IFixture AddThrowingRetryLogic(this IFixture fixture)
    {
        fixture.Register<IAsyncRetryLogic>(
            () => new SimpleRetryLogic(_ => throw new QueryApiTestException("Retry failed")));

        return fixture;
    }
}

internal class QueryApiTestException(string message) : Exception(message);
