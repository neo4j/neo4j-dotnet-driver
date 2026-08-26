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
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Time;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class FakeTimeHandlerTests
{
    [Fact]
    public async Task Install_installs_the_fake_clock_and_acks()
    {
        var autoMocker = AutoMocker.ForTesting<FakeTimeInstallHandler>();
        var handler = autoMocker.CreateInstance<FakeTimeInstallHandler>();

        await handler.ProcessAsync(new FakeTimeInstallRequest());

        autoMocker.GetMock<IFakeTimeService>().Verify(s => s.Install(), Times.Once);
        autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(new FakeTimeAckResponse()), Times.Once);
    }

    [Fact]
    public async Task Tick_advances_the_fake_clock_by_the_increment_and_acks()
    {
        var autoMocker = AutoMocker.ForTesting<FakeTimeTickHandler>();
        var handler = autoMocker.CreateInstance<FakeTimeTickHandler>();

        await handler.ProcessAsync(new FakeTimeTickRequest { IncrementMs = 1_500 });

        autoMocker.GetMock<IFakeTimeService>().Verify(s => s.Tick(1_500), Times.Once);
        autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(new FakeTimeAckResponse()), Times.Once);
    }

    [Fact]
    public async Task Uninstall_restores_the_real_clock_and_acks()
    {
        var autoMocker = AutoMocker.ForTesting<FakeTimeUninstallHandler>();
        var handler = autoMocker.CreateInstance<FakeTimeUninstallHandler>();

        await handler.ProcessAsync(new FakeTimeUninstallRequest());

        autoMocker.GetMock<IFakeTimeService>().Verify(s => s.Uninstall(), Times.Once);
        autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(new FakeTimeAckResponse()), Times.Once);
    }
}
