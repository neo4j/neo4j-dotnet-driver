// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Xunit;
using Path = System.IO.Path;

namespace Neo4j.Driver.Tests.Connector
{
    public class TrustOnFirstUseTests
    {
        public class Constructor
        {
            [Fact]
            public void ShouldNotCreateFileIfKnownHostFileDoesNotExist()
            {
                var fileName = Path.GetTempPath() + Guid.NewGuid() + ".tmp";
                var trustStrategy = new TrustOnFirstUse(null, fileName);

                File.Exists(fileName).Should().BeFalse();
                trustStrategy.KnownHost.Count.Should().Be(0);
            }

            [Fact]
            public void ShouldLoadFromKnownHostIfFileExists()
            {
                var fileName = Path.GetTempPath() + Guid.NewGuid() + ".tmp";
                try
                {
                    using (var fileWriter = File.CreateText(fileName))
                    {
                        fileWriter.WriteLine("#this line should be ignored!");
                        fileWriter.WriteLine("serverId fingerprint");
                    }
                    File.Exists(fileName).Should().BeTrue();

                    var trustStrategy = new TrustOnFirstUse(null, fileName);

                    trustStrategy.KnownHost.Count.Should().Be(1);
                    trustStrategy.KnownHost["serverId"].Should().Be("fingerprint");
                }
                finally
                {
                    File.Delete(fileName);

                }
            }
        }

        public class ValidateServerCertificate
        {
            private static readonly string ServerId = "bolt://123:45/";
            private static readonly string ServerCertText = "01 02 03 04 05 06 07 08 09 00";
            private static readonly string Fingerprint =
                "3AD3F36979450D4F53366244ECF1010F4F9121D6888285FF14104FD5ADED85D48AA171BF1E33A112602F92B7A7088B298789012FB87B9056321241A19FB74E0B";

            [Fact]
            public void ShouldSaveNewServerIdToKnownHostsFileAndReturnTrue()
            {
                var fileName = Path.GetTempPath() + Guid.NewGuid() + ".tmp";
                try
                {
                    var trustStrategy = new TrustOnFirstUse(null, fileName);
                    var mockCert = new Mock<X509Certificate>();
                    mockCert.Setup(x => x.GetCertHash()).Returns(ServerCertText.ToByteArray());
                    var valid = trustStrategy.ValidateServerCertificate(new Uri(ServerId), mockCert.Object, SslPolicyErrors.None);

                    valid.Should().BeTrue();
                    trustStrategy.KnownHost.Count.Should().Be(1);
                    trustStrategy.KnownHost.Keys.Single().Should().Be(ServerId);
                    trustStrategy.KnownHost.Values.Single().Should().Be(Fingerprint);
                }
                finally
                {
                    File.Delete(fileName);
                }
            }

            [Fact]
            public void ShouldReturnTrueIfServerIdIsTheSameAsTheOneSaved()
            {
                var fileName = Path.GetTempPath() + Guid.NewGuid() + ".tmp";
                try
                {
                    // Given a knownHost file with ServerId Fingerprint in it
                    using (var fileWriter = File.CreateText(fileName))
                    {
                        fileWriter.WriteLine("#this line should be ignored!");
                        fileWriter.WriteLine($"{ServerId} {Fingerprint}");
                    }
                    File.Exists(fileName).Should().BeTrue();

                    var trustStrategy = new TrustOnFirstUse(null, fileName);

                    trustStrategy.KnownHost.Count.Should().Be(1);
                    trustStrategy.KnownHost[ServerId].Should().Be(Fingerprint);

                    // When
                    var mockCert = new Mock<X509Certificate>();
                    mockCert.Setup(x => x.GetCertHash()).Returns(ServerCertText.ToByteArray());
                    var valid = trustStrategy.ValidateServerCertificate(new Uri(ServerId), mockCert.Object, SslPolicyErrors.None);

                    // Then
                    valid.Should().BeTrue();
                    trustStrategy.KnownHost.Count.Should().Be(1);
                    trustStrategy.KnownHost.Keys.Single().Should().Be(ServerId);
                    trustStrategy.KnownHost.Values.Single().Should().Be(Fingerprint);
                }
                finally
                {
                    File.Delete(fileName);
                }
            }

            [Fact]
            public void ShouldReturnFalseIfServerIdIsDifferentFromTheOneSaved()
            {
                var fileName = Path.GetTempPath() + Guid.NewGuid() + ".tmp";
                try
                {
                    var savedFingerprint = "SavedFingerprint";
                    // Given a knownHost file with ServerId Fingerprint in it
                    using (var fileWriter = File.CreateText(fileName))
                    {
                        fileWriter.WriteLine("#this line should be ignored!");
                        fileWriter.WriteLine($"{ServerId} {savedFingerprint}");
                    }
                    File.Exists(fileName).Should().BeTrue();

                    var trustStrategy = new TrustOnFirstUse(null, fileName);

                    trustStrategy.KnownHost.Count.Should().Be(1);
                    trustStrategy.KnownHost[ServerId].Should().Be(savedFingerprint);

                    // When
                    var mockCert = new Mock<X509Certificate>();
                    mockCert.Setup(x => x.GetCertHash()).Returns(ServerCertText.ToByteArray());
                    var valid = trustStrategy.ValidateServerCertificate(new Uri(ServerId), mockCert.Object, SslPolicyErrors.None);

                    // Then
                    valid.Should().BeFalse();
                    trustStrategy.KnownHost.Count.Should().Be(1);
                    trustStrategy.KnownHost.Keys.Single().Should().Be(ServerId);
                    trustStrategy.KnownHost.Values.Single().Should().Be(savedFingerprint);
                }
                finally
                {
                    File.Delete(fileName);
                }
            }
        }
    }
}
