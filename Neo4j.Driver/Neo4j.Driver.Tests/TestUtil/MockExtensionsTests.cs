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
using Moq;
using Xunit;

namespace Neo4j.Driver.Tests.TestUtil;

public class MockExtensionsTests
{
    [Fact]
    public void ShouldSetupSequentialReturns()
    {
        var mock = new Mock<IntGetter>();
        mock.SetupSequence(x => x.Value).ReturnsSequence(new[] { 1, 2, 3 });

        mock.Object.Value.Should().Be(1);
        mock.Object.Value.Should().Be(2);
        mock.Object.Value.Should().Be(3);
    }

    public interface IntGetter
    {
        int Value { get; }
    }
}
