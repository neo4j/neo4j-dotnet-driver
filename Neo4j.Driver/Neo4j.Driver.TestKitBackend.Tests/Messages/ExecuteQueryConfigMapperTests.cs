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
using Moq.AutoMock;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class ExecuteQueryConfigMapperTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<ExecuteQueryConfigMapper>();

    private QueryConfig Map(ExecuteQueryConfig config)
    {
        var mapper = _autoMocker.CreateInstance<ExecuteQueryConfigMapper>();
        return mapper.Map(config);
    }

    [Fact]
    public void Defaults_to_writers_routing_when_absent()
    {
        Map(new ExecuteQueryConfig()).Routing.Should().Be(RoutingControl.Writers);
    }

    [Fact]
    public void Maps_writer_routing_explicitly()
    {
        Map(new ExecuteQueryConfig { Routing = "w" }).Routing.Should().Be(RoutingControl.Writers);
    }

    [Fact]
    public void Maps_reader_routing()
    {
        Map(new ExecuteQueryConfig { Routing = "r" }).Routing.Should().Be(RoutingControl.Readers);
    }

    [Fact]
    public void Maps_database_and_impersonated_user()
    {
        var config = Map(new ExecuteQueryConfig { Database = "neo4j", ImpersonatedUser = "someone" });

        config.Database.Should().Be("neo4j");
        config.ImpersonatedUser.Should().Be("someone");
    }

    [Fact]
    public void Uses_the_default_bookmark_manager_when_the_id_is_absent()
    {
        var config = Map(new ExecuteQueryConfig());

        config.BookmarkManager.Should().BeNull();
        config.EnableBookmarkManager.Should().BeTrue();
    }

    [Fact]
    public void Disables_the_bookmark_manager_when_the_id_is_negative_one()
    {
        var config = Map(new ExecuteQueryConfig { BookmarkManagerId = "-1" });

        config.BookmarkManager.Should().BeNull();
        config.EnableBookmarkManager.Should().BeFalse();
    }

    [Fact]
    public void Resolves_the_bookmark_manager_by_id()
    {
        var bookmarkManagerMock = new Mock<IBookmarkManager>();
        _autoMocker.GetMock<IRegistry>()
            .Setup(r => r.Get<IBookmarkManager>("bm-1"))
            .Returns(new RegistryObject<IBookmarkManager>("bm-1", bookmarkManagerMock.Object));

        var config = Map(new ExecuteQueryConfig { BookmarkManagerId = "bm-1" });

        config.BookmarkManager.Should().BeSameAs(bookmarkManagerMock.Object);
        config.EnableBookmarkManager.Should().BeTrue();
    }

    [Fact]
    public void Maps_the_tx_metadata()
    {
        var txMeta = new Dictionary<string, ICypherValue> { ["k"] = new CypherString("v") };
        var mapped = new Dictionary<string, object> { ["k"] = "v" };
        _autoMocker.GetMock<ICypherToNativeMapper>().Setup(m => m.Map(txMeta)).Returns(mapped);

        var config = Map(new ExecuteQueryConfig { TxMeta = txMeta });

        config.TransactionConfig.Metadata.Should().Equal(mapped);
    }

    [Fact]
    public void Does_not_touch_the_tx_metadata_when_absent()
    {
        Map(new ExecuteQueryConfig()).TransactionConfig.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Maps_the_timeout()
    {
        var config = Map(new ExecuteQueryConfig { Timeout = 17 });

        config.TransactionConfig.Timeout.Should().Be(TimeSpan.FromMilliseconds(17));
    }

    [Fact]
    public void Does_not_touch_the_timeout_when_absent()
    {
        Map(new ExecuteQueryConfig()).TransactionConfig.Timeout.Should().BeNull();
    }

    [Fact]
    public void Maps_the_authorization_token()
    {
        var config = Map(
            new ExecuteQueryConfig { AuthorizationToken = new AuthorizationToken("basic", "neo4j", "pass") });

        config.AuthToken.Should().BeAssignableTo<AuthToken>();
        var token = (AuthToken)config.AuthToken;
        token.Content["scheme"].Should().Be("basic");
        token.Content["principal"].Should().Be("neo4j");
        token.Content["credentials"].Should().Be("pass");
    }

    [Fact]
    public void Does_not_set_an_authorization_token_when_absent()
    {
        Map(new ExecuteQueryConfig()).AuthToken.Should().BeNull();
    }
}
