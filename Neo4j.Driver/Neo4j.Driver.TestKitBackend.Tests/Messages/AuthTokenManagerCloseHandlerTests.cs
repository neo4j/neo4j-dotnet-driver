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
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class AuthTokenManagerCloseHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<AuthTokenManagerCloseHandler>();

    [Fact]
    public async Task Removes_the_manager_from_the_registry_and_responds_with_its_id()
    {
        var handler = _autoMocker.CreateInstance<AuthTokenManagerCloseHandler>();

        await handler.ProcessAsync(new AuthTokenManagerCloseRequest { Id = "manager-1" });

        _autoMocker.GetMock<IRegistry>().Verify(r => r.Remove("manager-1"), Times.Once);
        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(new AuthTokenManagerResponse("manager-1")), Times.Once);
    }
}
