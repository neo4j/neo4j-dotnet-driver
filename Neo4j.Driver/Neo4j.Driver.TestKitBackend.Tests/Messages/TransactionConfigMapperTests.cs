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
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Types;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Messages;

public class TransactionConfigMapperTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<TransactionConfigMapper>();

    private static TransactionConfig Apply(Action<TransactionConfigBuilder> configure)
    {
        var config = new TransactionConfig();
        configure(new TransactionConfigBuilder(Mock.Of<INeo4jLogger>(), config));
        return config;
    }

    [Fact]
    public void Applies_the_mapped_tx_metadata_when_present()
    {
        var txMeta = new Dictionary<string, ICypherValue> { ["k"] = new CypherString("v") };
        var mapped = new Dictionary<string, object> { ["k"] = "v" };
        _autoMocker.GetMock<ICypherToNativeMapper>().Setup(m => m.Map(txMeta)).Returns(mapped);

        var mapper = _autoMocker.CreateInstance<TransactionConfigMapper>();
        var configure = mapper.Map(txMeta, Optional<long?>.Absent);

        Apply(configure).Metadata.Should().BeEquivalentTo(mapped);
    }

    [Fact]
    public void Does_not_touch_metadata_when_tx_meta_is_absent()
    {
        var mapper = _autoMocker.CreateInstance<TransactionConfigMapper>();
        var configure = mapper.Map(null, Optional<long?>.Absent);

        Apply(configure).Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_touch_the_timeout_when_absent()
    {
        var mapper = _autoMocker.CreateInstance<TransactionConfigMapper>();
        var configure = mapper.Map(null, Optional<long?>.Absent);

        Apply(configure).Timeout.Should().BeNull();
    }

    [Fact]
    public void Applies_an_explicit_null_timeout()
    {
        var mapper = _autoMocker.CreateInstance<TransactionConfigMapper>();
        var configure = mapper.Map(null, Optional<long?>.Specified(null));

        Apply(configure).Timeout.Should().BeNull();
    }

    [Fact]
    public void Applies_the_timeout_in_milliseconds_when_specified()
    {
        var mapper = _autoMocker.CreateInstance<TransactionConfigMapper>();
        var configure = mapper.Map(null, Optional<long?>.Specified(17));

        Apply(configure).Timeout.Should().Be(TimeSpan.FromMilliseconds(17));
    }
}
