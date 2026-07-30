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
using Neo4j.Driver.TestKitBackend.Messages;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class GetFeaturesHandlerTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<GetFeaturesHandler>();

    [Fact]
    public async Task Responds_with_the_feature_list()
    {
        var handler = _autoMocker.CreateInstance<GetFeaturesHandler>();

        var response = await handler.ProcessAsync(new GetFeaturesRequest());

        response.Should().BeOfType<FeatureListResponse>();
    }
}
