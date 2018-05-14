// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver.Internal;
using System.CodeDom;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Neo4j.Driver.Internal.Connector.Trust;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Sdk;

namespace Neo4j.Driver.Tests.Connector
{
    public class EncryptionManagerTests
    {

        [Fact]
        public void ShouldNotCreateTrustManagerIfEncryptionDisabled()
        {
            var encryption =
                new EncryptionManager(EncryptionLevel.None, TrustStrategy.TrustAllCertificates, null, null);

            encryption.TrustManager.Should().BeNull();
        }

        [Fact]
        public void ShouldCreateCorrectTrustManagerForTrustAllCertificates()
        {
            var encryption =
                new EncryptionManager(EncryptionLevel.Encrypted, TrustStrategy.TrustAllCertificates, null, null);

            encryption.TrustManager.Should().NotBeNull().And.BeOfType<InsecureTrustManager>();
        }

        [Fact]
        public void ShouldCreateCorrectTrustManagerForTrustSystemCaSignedCertificates()
        {
            var encryption =
                new EncryptionManager(EncryptionLevel.Encrypted, TrustStrategy.TrustSystemCaSignedCertificates, null, null);

            encryption.TrustManager.Should().NotBeNull().And.BeOfType<ChainTrustManager>();
        }

        [Fact]
        public void ShouldUseProvidedTrustManager()
        {
            var encryption =
                new EncryptionManager(EncryptionLevel.Encrypted, TrustStrategy.TrustSystemCaSignedCertificates, new CustomTrustManager(), null);

            encryption.TrustManager.Should().NotBeNull().And.BeOfType<CustomTrustManager>();
        }

        private class CustomTrustManager: TrustManager
        {
            public override bool ValidateServerCertificate(Uri uri, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                throw new NotImplementedException();
            }
        }
    }
}