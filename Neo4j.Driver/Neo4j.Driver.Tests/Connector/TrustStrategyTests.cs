// Copyright (c) 2002-2018 "Neo4j,"
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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.Connector
{
    public class TrustStrategyTests
    {
        
        public class TrustAll
        {

            [Fact]
            public void ShouldTrust()
            {
                var logger = new Mock<ILogger>(MockBehavior.Strict);
                var strategy = new TrustAllCertificates(logger.Object);

                var result = strategy.ValidateServerCertificate(new Uri("bolt://localhost"), new X509Certificate(),
                    SslPolicyErrors.None);

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldTrustEvenOnPolicyErrors()
            {
                var logger = new Mock<ILogger>(MockBehavior.Strict);
                var strategy = new TrustAllCertificates(logger.Object);

                var result = strategy.ValidateServerCertificate(new Uri("bolt://localhost"), new X509Certificate(),
                    SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors);

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldCheckCertificateNotAvailable()
            {
                var logger = new Mock<ILogger>();
                var strategy = new TrustAllCertificates(logger.Object);

                var result = strategy.ValidateServerCertificate(new Uri("bolt://localhost"), new X509Certificate(),
                    SslPolicyErrors.RemoteCertificateNotAvailable);

                result.Should().BeFalse();
                logger.Verify(l => l.Error("Certificate not available.", null));
            }

        }

        public class TrustSystemCa
        {

            [Fact]
            public void ShouldTrust()
            {
                var logger = new Mock<ILogger>(MockBehavior.Strict);
                var strategy = new TrustSystemCaSignedCertificates(logger.Object);

                var result = strategy.ValidateServerCertificate(new Uri("bolt://localhost"), new X509Certificate(),
                    SslPolicyErrors.None);

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldCheckPolicyErrors()
            {
                var logger = new Mock<ILogger>();
                var strategy = new TrustSystemCaSignedCertificates(logger.Object);

                var result = strategy.ValidateServerCertificate(new Uri("bolt://localhost"), new X509Certificate(),
                    SslPolicyErrors.RemoteCertificateNotAvailable | SslPolicyErrors.RemoteCertificateChainErrors |
                    SslPolicyErrors.RemoteCertificateNameMismatch);

                result.Should().BeFalse();
                logger.Verify(l => l.Error("Certificate not available.", null));
                logger.Verify(l => l.Error("Certificate validation failed.", null));
                logger.Verify(l => l.Error("Server name mismatch.", null));
            }

        }

    }
}