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

#nullable enable

using FluentAssertions;
using Neo4j.Driver.Internal.QueryApi;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

public class QueryApiMediaVersionTests
{
    [Fact]
    public void ToMediaTypeString_RendersV1_0()
    {
        QueryApiMediaVersion.V1_0.ToMediaTypeString().Should().Be("application/vnd.neo4j.query.v1.0");
    }

    [Fact]
    public void ToMediaTypeString_RendersV1_1()
    {
        QueryApiMediaVersion.V1_1.ToMediaTypeString().Should().Be("application/vnd.neo4j.query.v1.1");
    }
}
