// Copyright (c) 2002-2020 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver;
using Neo4j.Driver.Internal.Connector.Trust;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class ConfigTests
    {
        public class DefaultConfigTests
        {
            [Fact]
            public void DefaultConfigShouldGiveCorrectValueBack()
            {
                var config = Config.Default;
                config.EncryptionLevel.Should().Be(EncryptionLevel.None);
                config.TrustManager.Should().BeNull();
                config.Logger.Should().BeOfType<NullLogger>();
                config.MaxIdleConnectionPoolSize.Should().Be(500);
                config.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(30));
            }

            [Fact]
            public void ShouldUseMaxConnectionValueIfMaxIdleValueIsNotSpecified()
            {
                var config = new Config {MaxConnectionPoolSize = 50};
                config.MaxConnectionPoolSize.Should().Be(50);
                config.MaxIdleConnectionPoolSize.Should().Be(50);
            }

            [Fact]
            public void ShouldSetMaxIdleValueWhenSetSeparately()
            {
                var config = new Config {MaxIdleConnectionPoolSize = 20, MaxConnectionPoolSize = 50};
                config.MaxConnectionPoolSize.Should().Be(50);
                config.MaxIdleConnectionPoolSize.Should().Be(20);
            }
        }

        public class ConfigBuilderTests
        {
            [Fact]
            public void ShouldUseDefaultValueIfNotSpecified()
            {
                var config = new Config {EncryptionLevel = EncryptionLevel.Encrypted};

                config.EncryptionLevel.Should().Be(EncryptionLevel.Encrypted);
                config.TrustManager.Should().BeNull();
                config.Logger.Should().BeOfType<NullLogger>();
                config.MaxIdleConnectionPoolSize.Should().Be(500);
            }

            [Fact]
            public void ShouldUseMaxConnectionValueIfMaxIdleValueIsNotSpecified()
            {
                var config = Config.Builder.WithMaxConnectionPoolSize(50).Build();
                config.MaxConnectionPoolSize.Should().Be(50);
                config.MaxIdleConnectionPoolSize.Should().Be(50);
            }

            [Fact]
            public void ShouldSetMaxIdleValueWhenSetSeparately()
            {
                var config = Config.Builder.WithMaxConnectionPoolSize(50).WithMaxIdleConnectionPoolSize(20).Build();
                config.MaxConnectionPoolSize.Should().Be(50);
                config.MaxIdleConnectionPoolSize.Should().Be(20);
            }

            [Fact]
            public void WithLoggingShouldModifyTheSingleValue()
            {
                var config = Config.Builder.WithLogger(null).Build();
                config.EncryptionLevel.Should().Be(EncryptionLevel.None);
                config.TrustManager.Should().BeNull();
                config.Logger.Should().BeNull();
                config.MaxIdleConnectionPoolSize.Should().Be(500);
            }

            [Fact]
            public void WithPoolSizeShouldModifyTheSingleValue()
            {
                var config = Config.Builder.WithMaxIdleConnectionPoolSize(3).Build();
                config.EncryptionLevel.Should().Be(EncryptionLevel.None);
                config.TrustManager.Should().BeNull();
                config.Logger.Should().BeOfType<NullLogger>();
                config.MaxIdleConnectionPoolSize.Should().Be(3);
            }

            [Fact]
            public void WithEncryptionLevelShouldModifyTheSingleValue()
            {
                var config = Config.Builder.WithEncryptionLevel(EncryptionLevel.None).Build();
                config.EncryptionLevel.Should().Be(EncryptionLevel.None);
                config.TrustManager.Should().BeNull();
                config.Logger.Should().BeOfType<NullLogger>();
                config.MaxIdleConnectionPoolSize.Should().Be(500);
            }

            [Fact]
            public void WithTrustManagerShouldModifyTheSingleValue()
            {
                var config = Config.Builder.WithTrustManager(TrustManager.CreateChainTrust()).Build();
                config.EncryptionLevel.Should().Be(EncryptionLevel.None);
                config.TrustManager.Should().BeOfType<ChainTrustManager>();
                config.Logger.Should().BeOfType<NullLogger>();
                config.MaxIdleConnectionPoolSize.Should().Be(500);
            }

            [Fact]
            public void ChangingNewConfigShouldNotAffectOtherConfig()
            {
                var config = Config.Default;
                var config1 = Config.Builder.WithMaxIdleConnectionPoolSize(3).Build();
                var config2 = Config.Builder.WithLogger(null).Build();

                config2.Logger.Should().BeNull();
                config2.MaxIdleConnectionPoolSize.Should().Be(500);

                config1.MaxIdleConnectionPoolSize.Should().Be(3);
                config1.Logger.Should().BeOfType<NullLogger>();

                config.EncryptionLevel.Should().Be(EncryptionLevel.None);
                config.TrustManager.Should().BeNull();
                config.Logger.Should().BeOfType<NullLogger>();
                config.MaxIdleConnectionPoolSize.Should().Be(500);
            }
        }
    }
}