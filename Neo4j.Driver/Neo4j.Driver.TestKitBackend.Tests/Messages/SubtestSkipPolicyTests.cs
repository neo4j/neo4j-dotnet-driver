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
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class SubtestSkipPolicyTests
{
    private const string ZonedTimeTestName = "stub.http_query.datatypes.test_temporal.TestTemporal.test_zoned_time";

    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<SubtestSkipPolicy>();

    private readonly CypherTime _value =
        new() { Hour = 0, Minute = 0, Second = 0, Nanosecond = 0, UtcOffsetS = -86400 };

    [Fact]
    public void Skips_a_zoned_time_subtest_whose_argument_cannot_be_mapped_with_the_mapping_failure_as_the_reason()
    {
        var failure = new ArgumentOutOfRangeException("offsetSeconds", "offset out of range");
        _autoMocker.GetMock<ICypherToNativeMapper>()
            .Setup(m => m.Map(_value))
            .Throws(failure);

        var policy = _autoMocker.CreateInstance<SubtestSkipPolicy>();

        var skipped = policy.TryGetSkipReason(ZonedTimeTestName, Arguments(), out var reason);

        skipped.Should().BeTrue();
        reason.Should().Be(failure.Message);
    }

    [Fact]
    public void Runs_a_zoned_time_subtest_whose_argument_can_be_mapped()
    {
        _autoMocker.GetMock<ICypherToNativeMapper>()
            .Setup(m => m.Map(_value))
            .Returns(new OffsetTime(0, 0, 0, 0));

        var policy = _autoMocker.CreateInstance<SubtestSkipPolicy>();

        var skipped = policy.TryGetSkipReason(ZonedTimeTestName, Arguments(), out _);

        skipped.Should().BeFalse();
    }

    [Fact]
    public void Runs_a_subtest_of_any_other_test_even_when_its_argument_cannot_be_mapped()
    {
        _autoMocker.GetMock<ICypherToNativeMapper>()
            .Setup(m => m.Map(_value))
            .Throws(new ArgumentOutOfRangeException("offsetSeconds", "offset out of range"));

        var policy = _autoMocker.CreateInstance<SubtestSkipPolicy>();

        var skipped = policy.TryGetSkipReason("stub.some.other.TestCase.test_something", Arguments(), out _);

        skipped.Should().BeFalse();
    }

    private Dictionary<string, ICypherValue> Arguments()
    {
        return new Dictionary<string, ICypherValue> { ["x"] = _value };
    }
}
