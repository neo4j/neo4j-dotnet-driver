#pragma warning disable CS0618 // Type or member is obsolete
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

using System.Collections.Generic;
using FluentAssertions;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Xunit;

namespace Neo4j.Driver.Tests.Internal.MessageHandling.Metadata;

public class GqlStatusObjectsAndNotificationsCollectorTests
{
    [Fact]
    public void ShouldNotCollectAnythingIfMetadataIsNull()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(null);

        var collected = collector.Collected;
        collected.Should().BeNull();
    }

    [Fact]
    public void ShouldNotCollectAnythingIfMetadataDoesNotContainStatuses()
    {
        var collector = new GqlStatusObjectsAndNotificationsCollector(true);
        collector.Collect(new Dictionary<string, object>());

        var collected = collector.Collected;
        collected.Should().NotBeNull();
        collected.Notifications.Should().BeNull();
        collected.GqlStatusObjects.Should().BeNull();
    }
}
