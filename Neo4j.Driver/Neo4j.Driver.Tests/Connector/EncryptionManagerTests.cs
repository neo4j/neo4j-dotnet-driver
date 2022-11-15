// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector.Trust;
using Xunit;

namespace Neo4j.Driver.Tests.Connector;

public class EncryptionManagerTests
{
    public class CreateFromConfigMethod
    {
        [Fact]
        public void ShouldNotCreateTrustManagerIfNotEncrypted()
        {
            var encryption =
                EncryptionManager.CreateFromConfig(EncryptionLevel.None, null, null);

            encryption.UseTls.Should().BeFalse();
            encryption.TrustManager.Should().BeNull();
        }

        [Fact]
        public void ShouldNotCreateTrustManagerIfEncryptedIsNull()
        {
            var encryption =
                EncryptionManager.CreateFromConfig(null, null, null);

            encryption.UseTls.Should().BeFalse();
            encryption.TrustManager.Should().BeNull();
        }

        [Fact]
        public void ShouldCreateDefaultTrustManagerIfEncrypted()
        {
            var encryption =
                EncryptionManager.CreateFromConfig(EncryptionLevel.Encrypted, null, null);

            encryption.UseTls.Should().BeTrue();
            encryption.TrustManager.Should().NotBeNull().And.BeOfType<ChainTrustManager>();
        }

        [Fact]
        public void ShouldUseProvidedTrustManager()
        {
            var encryption =
                EncryptionManager.CreateFromConfig(null, new CustomTrustManager(), null);

            encryption.UseTls.Should().BeFalse();
            encryption.TrustManager.Should().NotBeNull().And.BeOfType<CustomTrustManager>();
        }
    }

    public class CreateMethod
    {
        [Theory]
        [InlineData("bolt")]
        [InlineData("neo4j")]
        public void ShouldCreateDefaultWithoutConfig(string scheme)
        {
            var uri = new Uri($"{scheme}://localhost/?");
            var encryption =
                EncryptionManager.Create(uri, null, null, null);

            encryption.UseTls.Should().BeFalse();
            encryption.TrustManager.Should().BeNull();
        }

        [Theory]
        [InlineData("bolt")]
        [InlineData("neo4j")]
        public void ShouldCreateFromConfig(string scheme)
        {
            var uri = new Uri($"{scheme}://localhost/?");
            var encryption =
                EncryptionManager.Create(uri, EncryptionLevel.Encrypted, null, null);

            encryption.UseTls.Should().BeTrue();
            encryption.TrustManager.Should().BeOfType<ChainTrustManager>();
        }

        [Theory]
        [InlineData("bolt+s")]
        [InlineData("neo4j+s")]
        public void ShouldCreateChainTrustFromUri(string scheme)
        {
            var uri = new Uri($"{scheme}://localhost/?");
            var encryption =
                EncryptionManager.Create(uri, null, null, null);

            encryption.UseTls.Should().BeTrue();
            encryption.TrustManager.Should().BeOfType<ChainTrustManager>();
        }

        [Theory]
        [InlineData("bolt+ssc")]
        [InlineData("neo4j+ssc")]
        public void ShouldCreateInsecureTrustFromUri(string scheme)
        {
            var uri = new Uri($"{scheme}://localhost/?");
            var encryption =
                EncryptionManager.Create(uri, null, null, null);

            encryption.UseTls.Should().BeTrue();
            encryption.TrustManager.Should().BeOfType<InsecureTrustManager>();

            if (encryption.TrustManager is InsecureTrustManager insecureTrustManager)
            {
                insecureTrustManager.VerifyHostName.Should().BeFalse();
            }
        }

        [Theory]
        [InlineData("bolt+s", EncryptionLevel.None)]
        [InlineData("neo4j+s", EncryptionLevel.None)]
        [InlineData("bolt+ssc", EncryptionLevel.None)]
        [InlineData("neo4j+ssc", EncryptionLevel.None)]
        [InlineData("bolt+s", EncryptionLevel.Encrypted)]
        [InlineData("neo4j+s", EncryptionLevel.Encrypted)]
        [InlineData("bolt+ssc", EncryptionLevel.Encrypted)]
        [InlineData("neo4j+ssc", EncryptionLevel.Encrypted)]
        public void ShouldErrorIfEncryptionLevelNotNull(string scheme, EncryptionLevel level)
        {
            var uri = new Uri($"{scheme}://localhost/?");
            var ex = Record.Exception(() => EncryptionManager.Create(uri, level, null, null));

            ex.Should().BeOfType<ArgumentException>();
            ex.Message.Should().Contain("cannot both be set via uri scheme and driver configuration");
        }

        [Theory]
        [InlineData("bolt+s")]
        [InlineData("neo4j+s")]
        [InlineData("bolt+ssc")]
        [InlineData("neo4j+ssc")]
        public void ShouldErrorIfTrustManagerNotNull(string scheme)
        {
            var uri = new Uri($"{scheme}://localhost/?");
            var ex = Record.Exception(() => EncryptionManager.Create(uri, null, new CustomTrustManager(), null));

            ex.Should().BeOfType<ArgumentException>();
            ex.Message.Should().Contain("cannot both be set via uri scheme and driver configuration");
        }
    }

    private class CustomTrustManager : TrustManager
    {
        public override bool ValidateServerCertificate(
            Uri uri,
            X509Certificate2 certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            throw new NotImplementedException();
        }
    }
}
