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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Mapping;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.Tests.Mapping;

public class MappingConcurrencyTests(ITestOutputHelper testOutputHelper)
{
    private interface ITestTask
    {
        Task Start();
    }

    private class TestTask<T> : ITestTask
    {
        public Task Start()
        {
            return Task.Run(
                () =>
                {
                    for (var i = 0; i < 50; i++)
                    {
                        DefaultMapper.Get<T>();
                        DefaultMapper.Reset();
                    }
                });
        }
    }

    private record DummyType1(string Name, int Age);
    private record DummyType2(string Name, int Age);
    private record DummyType3(string Name, int Age);
    private record DummyType4(string Name, int Age);

    [Fact]
    public async void DefaultMapperShouldBeThreadSafe()
    {
        List<ITestTask> threads =
        [
            new TestTask<DummyType1>(),
            new TestTask<DummyType2>(),
            new TestTask<DummyType3>(),
            new TestTask<DummyType4>()
        ];

        // wait for all threads to finish
        await Task.WhenAll(threads.Select(t => t.Start()));

        testOutputHelper.WriteLine("All threads finished.");
    }
}
