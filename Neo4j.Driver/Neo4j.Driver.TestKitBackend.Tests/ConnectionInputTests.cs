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
using Neo4j.Driver.TestKitBackend.Protocol;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests;

public class ConnectionInputTests
{
    [Fact]
    public async Task Returns_the_json_between_request_sentinels()
    {
        var input = Input("#request begin\n" + """{"name":"GetFeatures"}""" + "\n#request end\n");

        (await input.ReadRequestAsync()).Should().Be("""{"name":"GetFeatures"}""");
    }

    [Fact]
    public async Task Returns_successive_requests_on_successive_calls()
    {
        var input = Input(
            "#request begin\n" + """{"name":"A"}""" + "\n#request end\n" +
            "#request begin\n" + """{"name":"B"}""" + "\n#request end\n");

        (await input.ReadRequestAsync()).Should().Be("""{"name":"A"}""");
        (await input.ReadRequestAsync()).Should().Be("""{"name":"B"}""");
    }

    [Fact]
    public async Task Returns_null_at_end_of_stream()
    {
        var input = Input("");

        (await input.ReadRequestAsync()).Should().BeNull();
    }

    private static ConnectionInput Input(string data) => new(new StringReader(data));
}
