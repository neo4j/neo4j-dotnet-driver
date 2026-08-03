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

using System.Text.Json;
using FluentAssertions;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ExecuteQueryConfigTests
{
    private static readonly JsonSerializerOptions Options =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static ExecuteQueryConfig Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ExecuteQueryConfig>(json, Options)!;
    }

    [Fact]
    public void Leaves_bookmarkManagerId_null_when_absent()
    {
        Deserialize("{}").BookmarkManagerId.Should().BeNull();
    }

    [Fact]
    public void Reads_a_numeric_bookmarkManagerId_disable_sentinel_as_a_string()
    {
        Deserialize("""{"bookmarkManagerId": -1}""").BookmarkManagerId.Should().Be("-1");
    }

    [Fact]
    public void Reads_a_string_bookmarkManagerId()
    {
        Deserialize("""{"bookmarkManagerId": "5"}""").BookmarkManagerId.Should().Be("5");
    }
}
