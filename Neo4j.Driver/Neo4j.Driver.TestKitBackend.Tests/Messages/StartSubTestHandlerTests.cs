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

using Moq;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class StartSubTestHandlerTests
{
    private const string TestName = "stub.http_query.datatypes.test_temporal.TestTemporal.test_zoned_time";

    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<StartSubTestHandler>();

    private readonly Dictionary<string, ICypherValue> _arguments = new()
    {
        ["x"] = new CypherTime { Hour = 0, Minute = 0, Second = 0, Nanosecond = 0, UtcOffsetS = -86400 }
    };

    [Fact]
    public async Task Returns_RunTest_when_the_skip_policy_has_no_reason()
    {
        var handler = _autoMocker.CreateInstance<StartSubTestHandler>();

        await handler.ProcessAsync(new StartSubTestRequest { TestName = TestName, SubtestArguments = _arguments });

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new RunTestResponse()), Times.Once);
    }

    [Fact]
    public async Task Returns_SkipTest_with_the_policys_reason_when_the_skip_policy_has_one()
    {
        var reason = "offset out of range";
        _autoMocker.GetMock<ISubtestSkipPolicy>()
            .Setup(p => p.TryGetSkipReason(TestName, _arguments, out reason))
            .Returns(true);

        var handler = _autoMocker.CreateInstance<StartSubTestHandler>();

        await handler.ProcessAsync(new StartSubTestRequest { TestName = TestName, SubtestArguments = _arguments });

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new SkipTestResponse(reason)), Times.Once);
    }
}
