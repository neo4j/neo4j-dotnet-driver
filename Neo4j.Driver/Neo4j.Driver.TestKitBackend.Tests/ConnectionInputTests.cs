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

using FluentAssertions;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Connection;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class ConnectionInputTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ConnectionInput>();

    private ConnectionInput Input(params string[] lines)
    {
        var remaining = new Queue<string>(lines);
        _autoMocker.GetMock<ILineReader>()
            .Setup(r => r.ReadLineAsync())
            .Returns(() => Task.FromResult(remaining.TryDequeue(out var line) ? line : null));
        return _autoMocker.CreateInstance<ConnectionInput>();
    }

    [Fact]
    public async Task Returns_the_json_between_request_sentinels()
    {
        var input = Input("#request begin", """{"name":"GetFeatures"}""", "#request end");

        (await input.ReadRequestAsync()).Should().Be("""{"name":"GetFeatures"}""");
    }

    [Fact]
    public async Task Returns_successive_requests_on_successive_calls()
    {
        var input = Input(
            "#request begin", """{"name":"A"}""", "#request end",
            "#request begin", """{"name":"B"}""", "#request end");

        (await input.ReadRequestAsync()).Should().Be("""{"name":"A"}""");
        (await input.ReadRequestAsync()).Should().Be("""{"name":"B"}""");
    }

    [Fact]
    public async Task Returns_null_at_end_of_stream()
    {
        var input = Input();

        (await input.ReadRequestAsync()).Should().BeNull();
    }
}
