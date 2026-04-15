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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Preview;
using Xunit;
using InternalDriver = Neo4j.Driver.Internal.Driver;

namespace Neo4j.Driver.Tests;

public class MultiAddressTests
{
    private static readonly ServerAddress Addr1 = ServerAddress.From("server1.example.com", 7687);
    private static readonly ServerAddress Addr2 = ServerAddress.From("server2.example.com", 7688);
    private static readonly ServerAddress Addr3 = ServerAddress.From("server3.example.com", 7689);

    public class Construction
    {
        [Fact]
        public void TwoArgCtorSetsProperties()
        {
            var ma = new MultiAddress("neo4j", new[] { Addr1, Addr2 });

            ma.Scheme.Should().Be("neo4j");
            ma.Query.Should().Be(string.Empty);
            ma.Addresses.Should().ContainInOrder(Addr1, Addr2);
        }

        [Fact]
        public void ThreeArgCtorSetsProperties()
        {
            var ma = new MultiAddress("neo4j+s", "region=eu", new[] { Addr1 });

            ma.Scheme.Should().Be("neo4j+s");
            ma.Query.Should().Be("region=eu");
            ma.Addresses.Should().ContainInOrder(Addr1);
        }

        [Fact]
        public void NullQueryDefaultsToEmptyString()
        {
            var ma = new MultiAddress("neo4j", null, new[] { Addr1 });

            ma.Query.Should().Be(string.Empty);
        }

        [Fact]
        public void AddressOrderIsPreserved()
        {
            var ma = new MultiAddress("neo4j", new[] { Addr3, Addr1, Addr2 });

            ma.Addresses.Should().ContainInOrder(Addr3, Addr1, Addr2);
        }

        [Fact]
        public void SingleAddressIsAccepted()
        {
            var ma = new MultiAddress("neo4j", new[] { Addr1 });

            ma.Addresses.Should().HaveCount(1);
            ma.Addresses[0].Should().Be(Addr1);
        }

        [Fact]
        public void AddressesListIsReadOnly()
        {
            var ma = new MultiAddress("neo4j", new[] { Addr1 });

            ma.Addresses.Should().BeAssignableTo<IReadOnlyList<ServerAddress>>();
        }

        [Fact]
        public void NullSchemeThrowsArgumentNullException()
        {
            var act = () => new MultiAddress(null, new[] { Addr1 });

            act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("scheme");
        }

        [Fact]
        public void NullAddressesThrowsArgumentNullException()
        {
            var act = () => new MultiAddress("neo4j", (IEnumerable<ServerAddress>)null);

            act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("addresses");
        }

        [Fact]
        public void NullAddressesInThreeArgCtorThrowsArgumentNullException()
        {
            var act = () => new MultiAddress("neo4j", "region=eu", null);

            act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("addresses");
        }

        [Fact]
        public void EmptyAddressListThrowsArgumentException()
        {
            var act = () => new MultiAddress("neo4j", Enumerable.Empty<ServerAddress>());

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("addresses");
        }
    }

    public class ToCanonicalUri
    {
        [Fact]
        public void UsesFirstAddressHostAndPort()
        {
            var ma = new MultiAddress("neo4j", new[] { Addr1, Addr2 });

            var uri = ma.ToCanonicalUri();

            uri.Host.Should().Be(Addr1.Host);
            uri.Port.Should().Be(Addr1.Port);
        }

        [Fact]
        public void SchemeIsPreserved()
        {
            var ma = new MultiAddress("neo4j+ssc", new[] { Addr1 });

            ma.ToCanonicalUri().Scheme.Should().Be("neo4j+ssc");
        }

        [Fact]
        public void QueryIsIncludedWhenPresent()
        {
            var ma = new MultiAddress("neo4j", "region=eu", new[] { Addr1 });

            ma.ToCanonicalUri().Query.Should().Contain("region=eu");
        }

        [Fact]
        public void QueryIsAbsentWhenEmpty()
        {
            var ma = new MultiAddress("neo4j", string.Empty, new[] { Addr1 });

            ma.ToCanonicalUri().Query.Should().BeEmpty();
        }
    }

    public class MultiAddressProviderTests
    {
        [Fact]
        public void GetReturnsUriForEachAddress()
        {
            var ma = new MultiAddress("neo4j", new[] { Addr1, Addr2, Addr3 });
            var provider = new MultiAddressProvider(ma);

            var uris = provider.Get();

            uris.Should().HaveCount(3);
        }

        [Fact]
        public void GetPreservesHostAndPort()
        {
            var ma = new MultiAddress("neo4j", new[] { Addr1, Addr2 });
            var provider = new MultiAddressProvider(ma);

            var uris = provider.Get().OrderBy(u => u.Host).ToList();

            uris.Should().Contain(u => u.Host == Addr1.Host && u.Port == Addr1.Port);
            uris.Should().Contain(u => u.Host == Addr2.Host && u.Port == Addr2.Port);
        }

        [Fact]
        public void GetIncludesQueryOnEachUri()
        {
            var ma = new MultiAddress("neo4j", "region=eu", new[] { Addr1, Addr2 });
            var provider = new MultiAddressProvider(ma);

            var uris = provider.Get().ToList();

            uris.Should().OnlyContain(u => u.Query.Contains("region=eu"));
        }

        [Fact]
        public void GetOmitsQueryWhenEmpty()
        {
            var ma = new MultiAddress("neo4j", string.Empty, new[] { Addr1 });
            var provider = new MultiAddressProvider(ma);

            var uris = provider.Get().ToList();

            uris.Should().OnlyContain(u => string.IsNullOrEmpty(u.Query));
        }

        [Fact]
        public void GetUsesSchemeFromMultiAddress()
        {
            var ma = new MultiAddress("neo4j+s", new[] { Addr1 });
            var provider = new MultiAddressProvider(ma);

            provider.Get().Should().OnlyContain(u => u.Scheme == "neo4j+s");
        }
    }

    public class GraphDatabaseDriverOverload
    {
        [Fact]
        public void NullMultiAddressThrowsArgumentNullException()
        {
            var act = () => GraphDatabase.Driver((MultiAddress)null, AuthTokens.None);

            act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("multiAddress");
        }

        [Fact]
        public void NullAuthTokenManagerThrowsArgumentNullException()
        {
            var ma = new MultiAddress("neo4j", new[] { Addr1 });

            var act = () => GraphDatabase.Driver(ma, (IAuthTokenManager)null, null);

            act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("authTokenManager");
        }

        [Fact]
        public void BoltSchemeWithMultipleAddressesThrowsArgumentException()
        {
            var ma = new MultiAddress("bolt", new[] { Addr1, Addr2 });

            var act = () => GraphDatabase.Driver(ma, AuthTokens.None);

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("multiAddress");
        }

        [Fact]
        public void BoltSchemeWithNonEmptyQueryThrowsArgumentException()
        {
            var ma = new MultiAddress("bolt", "region=eu", new[] { Addr1 });

            var act = () => GraphDatabase.Driver(ma, AuthTokens.None);

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("multiAddress");
        }

        [Fact]
        public void RoutingSchemeWithMultipleAddressesCreatesDriver()
        {
            var ma = new MultiAddress("neo4j", new[] { Addr1, Addr2 });

            using var driver = GraphDatabase.Driver(ma, AuthTokens.None);

            driver.Should().NotBeNull();
        }

        [Fact]
        public void RoutingSchemeCanonicalUriUsesFirstAddress()
        {
            var ma = new MultiAddress("neo4j", new[] { Addr1, Addr2 });

            using var driver = (InternalDriver)GraphDatabase.Driver(ma, AuthTokens.None);

            driver.Uri.Host.Should().Be(Addr1.Host);
            driver.Uri.Port.Should().Be(Addr1.Port);
        }

        [Fact]
        public void DirectSchemeWithSingleAddressCreatesDriver()
        {
            var ma = new MultiAddress("bolt", new[] { Addr1 });

            using var driver = GraphDatabase.Driver(ma, AuthTokens.None);

            driver.Should().NotBeNull();
        }

        [Fact]
        public void EncryptedRoutingSchemeCreatesDriver()
        {
            var ma = new MultiAddress("neo4j+s", new[] { Addr1, Addr2 });

            using var driver = GraphDatabase.Driver(ma, AuthTokens.None);

            driver.Should().NotBeNull();
        }

        [Fact]
        public void ConfigActionIsApplied()
        {
            var ma = new MultiAddress("neo4j", new[] { Addr1 });
            var configWasInvoked = false;

            using var driver = GraphDatabase.Driver(ma, AuthTokens.None, o => { configWasInvoked = true; });

            configWasInvoked.Should().BeTrue();
        }
    }
}
