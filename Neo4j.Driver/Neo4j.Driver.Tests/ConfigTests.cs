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
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Internal.Connector.Trust;
using Neo4j.Driver.Internal.Logging;
using Neo4j.Driver.Internal.Types;
using Xunit;

namespace Neo4j.Driver.Tests;

public class ConfigTests
{
    public class DefaultConfigTests
    {
        [Fact]
        public void DefaultConfigShouldGiveCorrectValueBack()
        {
            var config = new Config();
            config.EncryptionLevel.Should().Be(EncryptionLevel.None);
            config.TrustManager.Should().BeNull();
            config.Logger.Should().BeOfType<NullLogger>();
            config.MaxIdleConnectionPoolSize.Should().Be(100);
            config.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(30));
            config.TlsVersion.Should().Be(SslProtocols.Tls12);
            config.TlsNegotiator.Should().BeNull();
        }

        [Fact]
        public void ShouldUseMaxConnectionValueIfMaxIdleValueIsNotSpecified()
        {
            var config = new Config { MaxConnectionPoolSize = 50 };
            config.MaxConnectionPoolSize.Should().Be(50);
            config.MaxIdleConnectionPoolSize.Should().Be(50);
        }

        [Fact]
        public void ShouldSetMaxIdleValueWhenSetSeparately()
        {
            var config = new Config { MaxIdleConnectionPoolSize = 20, MaxConnectionPoolSize = 50 };
            config.MaxConnectionPoolSize.Should().Be(50);
            config.MaxIdleConnectionPoolSize.Should().Be(20);
        }

        [Fact]
        public void ShouldDefaultToNoEncryptionAndNoTrust()
        {
            var config = new Config();
            config.NullableEncryptionLevel.Should().BeNull();
            config.EncryptionLevel.Should().Be(EncryptionLevel.None);
            config.TrustManager.Should().BeNull();
        }

        [Fact]
        public void ShouldSetEncryptionAndTrust()
        {
            var config = new Config
            {
                EncryptionLevel = EncryptionLevel.None,
                TrustManager = null
            };

            config.NullableEncryptionLevel.Should().Be(EncryptionLevel.None);
            config.EncryptionLevel.Should().Be(EncryptionLevel.None);
            config.TrustManager.Should().BeNull();
        }
    }

    public class ConfigBuilderTests
    {
        [Fact]
        public void ShouldUseDefaultValueIfNotSpecified()
        {
            var config = new Config { EncryptionLevel = EncryptionLevel.Encrypted };

            config.EncryptionLevel.Should().Be(EncryptionLevel.Encrypted);
            config.TrustManager.Should().BeNull();
            config.Logger.Should().BeOfType<NullLogger>();
            config.MaxIdleConnectionPoolSize.Should().Be(100);
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
            var mockLogger = new Mock<ILogger>();
            var config = Config.Builder.WithLogger(mockLogger.Object).Build();
            config.EncryptionLevel.Should().Be(EncryptionLevel.None);
            config.TrustManager.Should().BeNull();
            config.Logger.Should().Be(mockLogger.Object);
            config.MaxIdleConnectionPoolSize.Should().Be(100);
        }

        [Fact]
        public void WithLoggingShouldRemainNullSafe()
        {
            var config = Config.Builder.WithLogger(null).Build();
            config.EncryptionLevel.Should().Be(EncryptionLevel.None);
            config.TrustManager.Should().BeNull();
            config.Logger.Should().Be(NullLogger.Instance);
            config.MaxIdleConnectionPoolSize.Should().Be(100);
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
        public void WithEncryptionLevelShouldModifyTheNullableValue()
        {
            var config = Config.Builder.WithEncryptionLevel(EncryptionLevel.None).Build();
            config.EncryptionLevel.Should().Be(EncryptionLevel.None);
            config.NullableEncryptionLevel.Should().Be(EncryptionLevel.None);
            config.TrustManager.Should().BeNull();
            config.Logger.Should().BeOfType<NullLogger>();
            config.MaxIdleConnectionPoolSize.Should().Be(100);
        }

        [Fact]
        public void WithTrustManagerShouldModifyTheSingleValue()
        {
            var config = Config.Builder.WithTrustManager(TrustManager.CreateChainTrust()).Build();
            config.EncryptionLevel.Should().Be(EncryptionLevel.None);
            config.TrustManager.Should().BeOfType<ChainTrustManager>();
            config.Logger.Should().BeOfType<NullLogger>();
            config.MaxIdleConnectionPoolSize.Should().Be(100);
        }

        [Fact]
        public void ChangingNewConfigShouldNotAffectOtherConfig()
        {
            var config = new Config();
            var config1 = Config.Builder.WithMaxIdleConnectionPoolSize(3).Build();
            var mockLogger = new Mock<ILogger>();
            var config2 = Config.Builder.WithLogger(mockLogger.Object).Build();

            config2.Logger.Should().Be(mockLogger.Object);
            config2.MaxIdleConnectionPoolSize.Should().Be(100);

            config1.MaxIdleConnectionPoolSize.Should().Be(3);
            config1.Logger.Should().BeOfType<NullLogger>();

            config.EncryptionLevel.Should().Be(EncryptionLevel.None);
            config.TrustManager.Should().BeNull();
            config.Logger.Should().BeOfType<NullLogger>();
            config.MaxIdleConnectionPoolSize.Should().Be(100);
        }

        [Fact]
        public void WithClientCertificateShouldModifyTheSingleValue()
        {
            var provider = new Mock<IClientCertificateProvider>();
            var config = Config.Builder.WithClientCertificateProvider(provider.Object).Build();
            config.EncryptionLevel.Should().Be(EncryptionLevel.None);
            config.TrustManager.Should().BeNull();
            config.Logger.Should().BeOfType<NullLogger>();
            config.MaxIdleConnectionPoolSize.Should().Be(100);
            config.ClientCertificateProvider.Should().Be(provider.Object);
        }

#if NET5_0_OR_GREATER
        [Fact]
        public void WithTlsVersionShouldModifyTheSingleValue()
        {
            var config = Config.Builder.WithTls13().Build();
            config.EncryptionLevel.Should().Be(EncryptionLevel.None);
            config.TrustManager.Should().BeNull();
            config.MaxIdleConnectionPoolSize.Should().Be(100);
            config.TlsVersion.Should().Be(SslProtocols.Tls13);
        }
#endif

        [Fact]
        public void WithTlsNegotiator_ShouldSetTlsNegotiator()
        {
            var mockTlsNegotiator = new Mock<ITlsNegotiator>();
            var configBuilder = new ConfigBuilder(new Config());

            configBuilder.WithTlsNegotiator(mockTlsNegotiator.Object);

            configBuilder.Build().TlsNegotiator.Should().Be(mockTlsNegotiator.Object);
        }

        [Fact]
        public void WithTlsNegotiatorDelegate_ShouldSetTlsNegotiator()
        {
            var configBuilder = new ConfigBuilder(new Config());

            configBuilder.WithTlsNegotiator((stream, host) => null);

            configBuilder.Build().TlsNegotiator.Should().BeOfType<DelegateTlsNegotiator>();
        }

        [Fact]
        public void WithTlsNegotiatorGeneric_ShouldSetTlsNegotiator()
        {
            var configBuilder = new ConfigBuilder(new Config());

            configBuilder.WithTlsNegotiator<MockTlsNegotiator>();

            configBuilder.Build().TlsNegotiator.Should().BeOfType<MockTlsNegotiator>();
        }

        [Theory]
        [InlineData(Classification.Hint, Category.Hint)]
        [InlineData(Classification.Unrecognized, Category.Unrecognized)]
        [InlineData(Classification.Unsupported, Category.Unsupported)]
        [InlineData(Classification.Performance, Category.Performance)]
        [InlineData(Classification.Deprecation, Category.Deprecation)]
        [InlineData(Classification.Security, Category.Security)]
        [InlineData(Classification.Topology, Category.Topology)]
        [InlineData(Classification.Schema, Category.Schema)]
        [InlineData(Classification.Generic, Category.Generic)]
        public void WithNotifications_ShouldSetCategoryWithClassification(
            Classification classification,
            Category category)
        {
            var configBuilder = new ConfigBuilder(new Config());

            configBuilder.WithNotifications(null, disabledClassifications: [classification]);

            var config = configBuilder.Build()
                .NotificationsConfig.Should()
                .BeOfType<NotificationsConfig>();

            config
                .Which
                .DisabledCategories.Should()
                .BeEquivalentTo([category]);

            config
                .Which
                .MinimumSeverity.Should()
                .Be(null);
        }

        [Theory]
        [InlineData(Category.Hint, Category.Hint)]
        [InlineData(Category.Unrecognized, Category.Unrecognized)]
        [InlineData(Category.Unsupported, Category.Unsupported)]
        [InlineData(Category.Performance, Category.Performance)]
        [InlineData(Category.Deprecation, Category.Deprecation)]
        [InlineData(Category.Security, Category.Security)]
        [InlineData(Category.Topology, Category.Topology)]
        [InlineData(Category.Schema, Category.Schema)]
        [InlineData(Category.Generic, Category.Generic)]
        public void WithNotifications_ShouldSetCategory(
            Category inCat,
            Category outCat)
        {
            var configBuilder = new ConfigBuilder(new Config());

            configBuilder.WithNotifications(null, [inCat]);

            var config = configBuilder.Build()
                .NotificationsConfig.Should()
                .BeOfType<NotificationsConfig>();

            config
                .Which
                .DisabledCategories.Should()
                .BeEquivalentTo([outCat]);

            config
                .Which
                .MinimumSeverity.Should()
                .Be(null);
        }

        [Fact]
        public void WithNotifications_ShouldSetMultipleCategories()
        {
            var configBuilder = new ConfigBuilder(new Config());

            configBuilder.WithNotifications(null, [Category.Deprecation, Category.Hint]);

            var config = configBuilder.Build()
                .NotificationsConfig.Should()
                .BeOfType<NotificationsConfig>();

            config
                .Which
                .DisabledCategories.Should()
                .BeEquivalentTo([Category.Deprecation, Category.Hint]);

            config
                .Which
                .MinimumSeverity.Should()
                .Be(null);
        }

        [Fact]
        public void WithNotifications_ShouldSetMultipleClassifications()
        {
            var configBuilder = new ConfigBuilder(new Config());

            configBuilder.WithNotifications(
                null,
                disabledClassifications: [Classification.Deprecation, Classification.Hint]);

            var config = configBuilder.Build()
                .NotificationsConfig.Should()
                .BeOfType<NotificationsConfig>();

            config
                .Which
                .DisabledCategories.Should()
                .BeEquivalentTo([Category.Deprecation, Category.Hint]);

            config
                .Which
                .MinimumSeverity.Should()
                .Be(null);
        }

        [Fact]
        public void WithNotifications_ShouldSetSeverity()
        {
            var configBuilder = new ConfigBuilder(new Config());

            configBuilder.WithNotifications(Severity.Information, Array.Empty<Category>());

            var config = configBuilder.Build()
                .NotificationsConfig.Should()
                .BeOfType<NotificationsConfig>();

            config
                .Which
                .DisabledCategories.Should()
                .BeEmpty();

            config
                .Which
                .MinimumSeverity.Should()
                .Be(Severity.Information);
        }

        [Fact]
        public void WithNotifications_ShouldSetSeverityWhenUsingClassification()
        {
            var configBuilder = new ConfigBuilder(new Config());

            configBuilder.WithNotifications(Severity.Warning, disabledClassifications: Array.Empty<Classification>());

            var config = configBuilder.Build()
                .NotificationsConfig.Should()
                .BeOfType<NotificationsConfig>();

            config
                .Which
                .DisabledCategories.Should()
                .BeEmpty();

            config
                .Which
                .MinimumSeverity.Should()
                .Be(Severity.Warning);
        }

        [Fact]
        public void WithNotifications_ShouldWorkWithSecondParameterNull()
        {
            var configBuilder = new ConfigBuilder(new Config());

            // this line would fail to compile before the fix
            configBuilder.WithNotifications(Severity.Warning);

            var config = configBuilder.Build()
                .NotificationsConfig.Should()
                .BeOfType<NotificationsConfig>();

            config
                .Which
                .DisabledCategories.Should()
                .BeNull();

            config
                .Which
                .MinimumSeverity.Should()
                .Be(Severity.Warning);
        }

        [Fact]
        public void WithNotifications_ShouldSetMultipleCategoriesAndClassifications()
        {
            var configBuilder = new ConfigBuilder(new Config());

            // specify both categories and classifications
            configBuilder.WithNotifications(
                Severity.Warning,
                [Category.Deprecation, Category.Hint],
                [Classification.Deprecation, Classification.Topology]);

            var config = configBuilder.Build()
                .NotificationsConfig.Should()
                .BeOfType<NotificationsConfig>();

            config
                .Which
                .DisabledCategories.Should()
                .BeEquivalentTo([Category.Deprecation, Category.Hint, Category.Topology]);

            config
                .Which
                .MinimumSeverity.Should()
                .Be(Severity.Warning);
        }

        [Fact]
        public void WithNotifications_ShouldHaveNullExclusions()
        {
            var configBuilder = new ConfigBuilder(new Config());

            // this line would fail to compile before the fix
            configBuilder.WithNotifications(Severity.Warning);

            var config = configBuilder.Build()
                .NotificationsConfig.Should()
                .BeOfType<NotificationsConfig>();

            config
                .Which
                .DisabledCategories.Should()
                .BeNull();

            config
                .Which
                .MinimumSeverity.Should()
                .Be(Severity.Warning);
        }

        private class MockTlsNegotiator : ITlsNegotiator
        {
            /// <inheritdoc/>
            public SslStream NegotiateTls(Uri uri, Stream stream)
            {
                return null;
            }
        }
    }
}
