// Copyright (c) "Neo4j"
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
using Org.BouncyCastle.Pkcs;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests.Direct
{
    public class CertificateTrustIT : DirectDriverTestBase
    {
        public CertificateTrustIT(ITestOutputHelper output, SingleServerFixture fixture) : base(output, fixture)
        {
            Server = fixture.SingleServerDbms;
            Pkcs12 = (Server as TestContainerServer)?.Pkcs12Store;
        }

        public ISingleServer Server { get; }
        public Pkcs12Store Pkcs12 { get; }
    
        [ShouldNotRunInTestKitFact]
        public async Task CertificateTrustManager_ShouldTrust()
        {
            await VerifySuccess(Server.BoltUri,
                new CertificateTrustManager(true, new[] {Pkcs12.GetDotnetCertificate()}),
                EncryptionLevel.None);
        }

        [ShouldNotRunInTestKitFact]
        public async Task CertificateTrustManager_ShouldNotTrustIfHostnameDiffers()
        {
            await VerifyFailure(new Uri("bolt://another.host.domain:7687"),
                new CertificateTrustManager(true, new[] {Pkcs12.GetDotnetCertificate()}));
        }

        [ShouldNotRunInTestKitFact]
        public Task CertificateTrustManager_ShouldTrustIfHostnameDiffersWhenHostnameVerificationIsDisabled()
        {
            return VerifySuccess(new Uri("bolt://another.host.domain:7687"), 
                new CertificateTrustManager(false, new[] {Pkcs12.GetDotnetCertificate()}));
        }
    
        [ShouldNotRunInTestKitFact]
        public async Task CertificateTrustManager_ShouldNotTrustIfNotValid()
        {
            var pkcs12 = CertificateUtils.CreateCert("localhost", DateTime.Now.AddYears(-1),
                DateTime.Now.AddDays(-1),
                null, null, null);
            await VerifyFailure(Server.BoltUri,
                new CertificateTrustManager(true, new[] {pkcs12.GetDotnetCertificate()}));
        }

        [ShouldNotRunInTestKitFact]
        public async Task CertificateTrustManager_ShouldNotTrustIfCertificateIsNotTrusted()
        {
            var pkcs12Untrusted = CertificateUtils.CreateCert("localhost", DateTime.Now.AddYears(-1),
                DateTime.Now.AddYears(1),
                null, null, null);

            await VerifyFailure(Server.BoltUri,
                new CertificateTrustManager(true, new[] {pkcs12Untrusted.GetDotnetCertificate()}));
        }

        [ShouldNotRunInTestKitFact]
        public async Task InsecureTrustManager_ShouldTrust()
        {
            await VerifySuccess(Server.BoltUri, new InsecureTrustManager(true), EncryptionLevel.None);
        }

        [ShouldNotRunInTestKitFact]
        public async Task InsecureTrustManager_ShouldNotTrustIfHostnameDiffers()
        {
            await VerifyFailure(new Uri("bolt://another.host.domain:7687"), new InsecureTrustManager(true));
        }

        [ShouldNotRunInTestKitFact]
        public async Task InsecureTrustManager_ShouldTrustIfHostnameDiffersWhenHostnameVerificationIsDisabled()
        {
            await VerifySuccess(new Uri("bolt://another.host.domain:7687"), new InsecureTrustManager(false),
                EncryptionLevel.None);
        }

        private async Task VerifyFailure(Uri target, TrustManager trustManager)
        {
            var ex = await Record.ExceptionAsync(() => TestConnectivity(target,
                Config.Builder.WithTrustManager(trustManager).WithEncryptionLevel(EncryptionLevel.Encrypted).Build()
            ));
            ex.Should().BeOfType<SecurityException>().Which.Message.Should()
                .Contain("Failed to establish encrypted connection with server");
        }

        private async Task VerifySuccess(Uri target, TrustManager trustManager,
            EncryptionLevel encryptionLevel = EncryptionLevel.Encrypted)
        {
            var ex = await Record.ExceptionAsync(() => TestConnectivity(target,
                Config.Builder.WithTrustManager(trustManager).WithEncryptionLevel(encryptionLevel).Build()
            ));
        
            ex.Should().BeNull();
        }

        private async Task TestConnectivity(Uri target, Config config)
        {
            using var driver = SetupWithCustomResolver(target, config);
            await using var session = driver.AsyncSession();

            var cursor = await session.RunAsync("RETURN 1");
            var records = await cursor.ToListAsync(r => r[0].As<int>());

            records.Should().BeEquivalentTo(1);
        }

        private IDriver SetupWithCustomResolver(Uri overridenUri, Config config)
        {
            var connectionSettings = new ConnectionSettings(overridenUri, Server.AuthToken, config);
            connectionSettings.SocketSettings.HostResolver =
                new CustomHostResolver(Server.BoltUri, connectionSettings.SocketSettings.HostResolver);
            var bufferSettings = new BufferSettings(config);
            var connectionFactory =
                new PooledConnectionFactory(connectionSettings, bufferSettings, config.Logger);

            return GraphDatabase.CreateDriver(overridenUri, config, connectionFactory);
        }

        private sealed class CustomHostResolver : IHostResolver
        {
            private readonly IHostResolver _original;
            private readonly Uri _target;

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
    }
}