﻿// Copyright (c) "Neo4j"
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

namespace Neo4j.Driver.IntegrationTests.Direct
{
    public class CertificateTrustIT : IClassFixture<CertificateTrustIT.CertificateTrustIntegrationTestFixture>
    {
        public StandAlone Server { get; }
        public Pkcs12Store Pkcs12 { get; }

        public CertificateTrustIT(CertificateTrustIntegrationTestFixture fixture)
        {
            Server = fixture.StandAlone;
            Pkcs12 = fixture.Pkcs12;
        }

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
        public async Task
            CertificateTrustManager_ShouldTrustIfHostnameDiffersWhenHostnameVerificationIsDisabled()
        {
            await VerifySuccess(new Uri("bolt://another.host.domain:7687"),
                new CertificateTrustManager(false, new[] {Pkcs12.GetDotnetCertificate()}));
        }

        [ShouldNotRunInTestKitFact]
        public async Task CertificateTrustManager_ShouldNotTrustIfNotValid()
        {
            try
            {
                var pkcs12 = CertificateUtils.CreateCert("localhost", DateTime.Now.AddYears(-1),
                    DateTime.Now.AddDays(-1),
                    null, null, null);

                Server.RestartServerWithCertificate(pkcs12);

                await VerifyFailure(Server.BoltUri,
                    new CertificateTrustManager(true, new[] {pkcs12.GetDotnetCertificate()}));
            }
            finally
            {
                Server.RestartServerWithCertificate(Pkcs12);
            }
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
            await VerifySuccess(new Uri("bolt://another.host.domain:7687"), new InsecureTrustManager(false), EncryptionLevel.None);
        }

        private async Task VerifyFailure(Uri target, TrustManager trustManager)
        {
            var ex = await Record.ExceptionAsync(() => TestConnectivity(target,
                Config.Builder.WithTrustManager(trustManager).WithEncryptionLevel(EncryptionLevel.Encrypted).Build()
            ));
            ex.Should().BeOfType<SecurityException>().Which.Message.Should()
                .Contain("Failed to establish encrypted connection with server");
        }

        private async Task VerifySuccess(Uri target, TrustManager trustManager, EncryptionLevel encryptionLevel = EncryptionLevel.Encrypted)
        {
            var ex = await Record.ExceptionAsync(() => TestConnectivity(target,
                Config.Builder.WithTrustManager(trustManager).WithEncryptionLevel(encryptionLevel).Build()
            ));
            ex.Should().BeNull();
        }

        private async Task TestConnectivity(Uri target, Config config)
        {
            using (var driver = SetupWithCustomResolver(target, config))
            {
                var session = driver.AsyncSession();
                try
                {
                    var cursor = await session.RunAsync("RETURN 1");
                    var records = await cursor.ToListAsync(r => r[0].As<int>());

                    records.Should().BeEquivalentTo(1);
                }
                finally
                {
                    await session.CloseAsync();
                }
            }
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

        // ReSharper disable once ClassNeverInstantiated.Global
        public class CertificateTrustIntegrationTestFixture : IDisposable
        {
            public StandAlone StandAlone { get; }
            public Pkcs12Store Pkcs12 { get; }

            ~CertificateTrustIntegrationTestFixture() => Dispose(false);

            public CertificateTrustIntegrationTestFixture()
            {
                if (!BoltkitHelper.IsBoltkitAvailable())
                {
                    return;
                }

                try
                {
                    Pkcs12 = CertificateUtils.CreateCert("localhost", DateTime.Now.AddYears(-1),
                        DateTime.Now.AddYears(1),
                        null, null, null);
                    StandAlone = new StandAlone(Pkcs12);
                }
                catch (Exception)
                {
                    Dispose();
                    throw;
                }
            }


            private bool _disposed = false;
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
			{
                if (_disposed)
                    return;

                if(disposing)
				{
                    //Dispose managed state (managed objects).
                    StandAlone?.Dispose();
                    StandAlone?.UpdateCertificate(Pkcs12);
                }

                _disposed = true;
            }
        }
    }
}