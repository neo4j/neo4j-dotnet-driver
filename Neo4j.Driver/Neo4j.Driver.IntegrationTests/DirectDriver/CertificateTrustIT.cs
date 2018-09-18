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
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Connector.Trust;
using Neo4j.Driver.V1;
using Org.BouncyCastle.Pkcs;
using Xunit;

namespace Neo4j.Driver.IntegrationTests
{
    public class CertificateTrustIT : IClassFixture<CertificateTrustIT.CertificateTrustIntegrationTestFixture>
    {
        public StandAlone Server { get; set; }
        public Pkcs12Store Pkcs12 { get; }

        public CertificateTrustIT(CertificateTrustIntegrationTestFixture fixture)
        {
            Server = fixture.StandAlone;
            Pkcs12 = fixture.Pkcs12;
        }

        [Fact]
        public void CertificateTrustManager_ShouldTrust()
        {
            VerifySuccess(Server.BoltUri, new CertificateTrustManager(true, new[] {Pkcs12.GetDotnetCertificate()}));
        }

        [Fact]
        public void CertificateTrustManager_ShouldNotTrustIfHostnameDiffers()
        {
            VerifyFailure(new Uri("bolt://another.host.domain:7687"),
                new CertificateTrustManager(true, new[] { Pkcs12.GetDotnetCertificate()}));
        }

        [Fact]
        public void
            CertificateTrustManager_ShouldTrustIfHostnameDiffersWhenHostnameVerificationIsDisabled()
        {
            VerifySuccess(new Uri("bolt://another.host.domain:7687"),
                new CertificateTrustManager(false, new[] { Pkcs12.GetDotnetCertificate()}));
        }

        [Fact]
        public void CertificateTrustManager_ShouldNotTrustIfNotValid()
        {
            try
            {
                var pkcs12 = CertificateUtils.CreateCert("localhost", DateTime.Now.AddYears(-1),
                    DateTime.Now.AddDays(-1),
                    null, null, null);

                Server.RestartServerWithCertificate(pkcs12);

                VerifyFailure(Server.BoltUri, new CertificateTrustManager(true, new[] {pkcs12.GetDotnetCertificate()}));
            }
            finally
            {
                Server.RestartServerWithCertificate(Pkcs12);
            }
        }

        [Fact]
        public void CertificateTrustManager_ShouldNotTrustIfCertificateIsNotTrusted()
        {
            var pkcs12Untrusted = CertificateUtils.CreateCert("localhost", DateTime.Now.AddYears(-1),
                DateTime.Now.AddYears(1),
                null, null, null);

            VerifyFailure(Server.BoltUri,
                new CertificateTrustManager(true, new[] {pkcs12Untrusted.GetDotnetCertificate()}));
        }

        [Fact]
        public void InsecureTrustManager_ShouldTrust()
        {
            VerifySuccess(Server.BoltUri, new InsecureTrustManager(true));
        }

        [Fact]
        public void InsecureTrustManager_ShouldNotTrustIfHostnameDiffers()
        {
            VerifyFailure(new Uri("bolt://another.host.domain:7687"), new InsecureTrustManager(true));
        }

        [Fact]
        public void InsecureTrustManager_ShouldTrustIfHostnameDiffersWhenHostnameVerificationIsDisabled()
        {
            VerifySuccess(new Uri("bolt://another.host.domain:7687"), new InsecureTrustManager(false));
        }

        private void VerifyFailure(Uri target, TrustManager trustManager)
        {
            var ex = Record.Exception(() => TestConnectivity(target,
                Config.Builder.WithTrustManager(trustManager).ToConfig()
            ));
            ex.Should().BeOfType<SecurityException>().Which.Message.Should()
                .Contain("Failed to establish encrypted connection with server");
        }

        private void VerifySuccess(Uri target, TrustManager trustManager)
        {
            var ex = Record.Exception(() => TestConnectivity(target,
                Config.Builder.WithTrustManager(trustManager).ToConfig()
            ));
            ex.Should().BeNull();
        }

        private void TestConnectivity(Uri target, Config config)
        {
            using (var driver = SetupWithCustomResolver(target, config))
            {
                using (var session = driver.Session())
                {
                    session.Run("RETURN 1").Consume();
                }
            }
        }

        private IDriver SetupWithCustomResolver(Uri overridenUri, Config config)
        {
            var connectionSettings = new ConnectionSettings(Server.AuthToken, config);
            connectionSettings.SocketSettings.HostResolver =
                new CustomHostResolver(Server.BoltUri, connectionSettings.SocketSettings.HostResolver);
            var bufferSettings = new BufferSettings(config);
            var connectionFactory = new PooledConnectionFactory(connectionSettings, bufferSettings, config.DriverLogger);

            return GraphDatabase.CreateDriver(overridenUri, config, connectionFactory);
        }

        private class CustomHostResolver : IHostResolver
        {
            private readonly Uri _target;
            private readonly IHostResolver _original;

            public CustomHostResolver(Uri target, IHostResolver original)
            {
                _target = target;
                _original = original;
            }

            public IPAddress[] Resolve(string hostname)
            {
                return _original.Resolve(_target.Host);
            }

            public Task<IPAddress[]> ResolveAsync(string hostname)
            {
                return _original.ResolveAsync(_target.Host);
            }
        }

        public class CertificateTrustIntegrationTestFixture : IDisposable
        {
            public StandAlone StandAlone { get; }
            public Pkcs12Store Pkcs12 { get; }

            public CertificateTrustIntegrationTestFixture()
            {
                if (!BoltkitHelper.IsBoltkitAvailable())
                {
                    return;
                }

                try
                {
                    Pkcs12 = CertificateUtils.CreateCert("localhost", DateTime.Now.AddYears(-1), DateTime.Now.AddYears(1),
                        null, null, null);
                    StandAlone = new StandAlone(Pkcs12);
                }
                catch (Exception)
                {
                    Dispose();
                    throw;
                }
            }

            public void Dispose()
            {
                StandAlone?.Dispose();
                StandAlone?.UpdateCertificate(null);
            }
        }
    }
}