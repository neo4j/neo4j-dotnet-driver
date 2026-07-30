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
using Neo4j.Driver.TestKitBackend.Logging;
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class StartTestHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<StartTestHandler>();

    public StartTestHandlerTests()
    {
        _autoMocker.Use<ILoggingContext>(new LoggingContext());
    }

    [Fact]
    public async Task Returns_RunTest_when_the_skip_policy_has_no_match()
    {
        var handler = _autoMocker.CreateInstance<StartTestHandler>();

        var response = await handler.ProcessAsync(new StartTestRequest { TestName = "some.test.name" });

        response.Should().BeOfType<RunTestResponse>();
    }

    [Fact]
    public async Task Returns_SkipTest_with_the_policys_reason_when_the_skip_policy_matches()
    {
        var reason = "known flaky";
        _autoMocker.GetMock<ISkipPolicy>()
            .Setup(p => p.TryGetSkipReason("some.test.name", out reason))
            .Returns(true);

        var handler = _autoMocker.CreateInstance<StartTestHandler>();

        var response = await handler.ProcessAsync(new StartTestRequest { TestName = "some.test.name" });

        response.Should().BeOfType<SkipTestResponse>().Subject.Reason.Should().Be(reason);
    }

    [Fact]
    public async Task Sets_TestName_on_the_logging_context()
    {
        var handler = _autoMocker.CreateInstance<StartTestHandler>();

        await handler.ProcessAsync(new StartTestRequest { TestName = "some.test.name" });

        _autoMocker.Get<ILoggingContext>().Current["test"].Should().Be("some.test.name");
    }
}
