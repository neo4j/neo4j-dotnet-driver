﻿// Copyright (c) 2002-2023 "Neo4j,"
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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Neo4j.Driver.IntegrationTests.Internals;
using Org.BouncyCastle.Pkcs;

namespace Neo4j.Driver.IntegrationTests
{
    internal sealed class TestContainerServer : ISingleServer
    {
        private readonly IContainer _container;
        private readonly string _dataPath;

        private TestContainerServer()
        {
            HttpUri = new Uri(Neo4jDefaultInstallation.HttpUri);
            BoltUri = new Uri(Neo4jDefaultInstallation.BoltUri);
            BoltRoutingUri = new Uri($"neo4j://{Neo4jDefaultInstallation.BoltHost}:{Neo4jDefaultInstallation.BoltPort}");
            AuthToken = AuthTokens.Basic(Neo4jDefaultInstallation.User, Neo4jDefaultInstallation.Password);

            Driver = GraphDatabase.Driver(BoltUri, AuthToken);
            _dataPath = Path.Combine(Environment.CurrentDirectory, "data");
        
            _container = TestContainerBuilder
                .ImageBase(4, 4, true)
                .WithPortBinding(int.Parse(Neo4jDefaultInstallation.BoltPort), 7687)
                .WithPortBinding(7474, 7474)
                .WithBindMount(_dataPath, "/var/local/")
                .WithEnvironment(BuildEnvVars())
                .WithWaitStrategy(
                    Wait.ForUnixContainer().UntilPortIsAvailable(int.Parse(Neo4jDefaultInstallation.BoltPort)))
                .Build();
        }

        public Uri HttpUri { get; }
        public Uri BoltUri { get; }
        public Uri BoltRoutingUri { get; }
        public IAuthToken AuthToken { get; }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            try
            {
                Driver.Dispose();
            }
            catch
            {
                // Ignore.
            }

            try
            {
                await _container.StopAsync();
            }
            catch
            {
                // Ignore.         
            }
        }

        public IDriver Driver { get; }
        public Pkcs12Store Pkcs12Store { get; private set; }


        private void WriteCerts()
        {
            TryCreatePath(null, _dataPath);
        
            var boltDir = TryCreatePath(_dataPath, "bolt");
            var trustedDir = TryCreatePath(boltDir, "trusted");

            var pubCertFilename = "public.crt";
            var keyFilename = "private.key";
            var pemFile = Path.Combine(boltDir, pubCertFilename);
            var trustedPemFile = Path.Combine(trustedDir, pubCertFilename);
            var keyFile = Path.Combine(boltDir, keyFilename);

            Pkcs12Store = CertificateUtils.CreateCert("localhost", DateTime.Now.AddYears(-1),
                DateTime.Now.AddYears(1),
                null, null, null);

            File.Delete(pemFile);
            File.Delete(trustedPemFile);
            File.Delete(keyFile);

            CertificateUtils.DumpPem(Pkcs12Store.GetCertificate(), pemFile);
            CertificateUtils.DumpPem(Pkcs12Store.GetCertificate(), trustedPemFile);
            CertificateUtils.DumpPem(Pkcs12Store.GetKey(), keyFile);
        }

        private string TryCreatePath(string root, string dir)
        {
            var p = root != null ? Path.Combine(root, dir) : dir;
            if (!Directory.Exists(p))
                Directory.CreateDirectory(p);

            return p;
        }

        private static Dictionary<string, string> BuildEnvVars()
        {
            var environment = new Dictionary<string, string>
            {
                ["NEO4J_ACCEPT_LICENSE_AGREEMENT"] = "yes",
                ["NEO4J_dbms_backup_enabled"] = "false",
                ["NEO4J_dbms_connector_bolt_tls__level"] = "OPTIONAL",
                ["NEO4J_dbms_ssl_policy_bolt_enabled"] = "true",
                ["NEO4J_dbms_ssl_policy_bolt_base__directory"] = "/var/local/bolt"
            };

            if (Neo4jDefaultInstallation.Password != "neo4j")
                environment.Add("NEO4J_AUTH",
                    $"{Neo4jDefaultInstallation.User}/{Neo4jDefaultInstallation.Password}");

            return environment;
        }

        public static async Task<ISingleServer> NewServerAsync()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var instance = new TestContainerServer();
            instance.WriteCerts();
            await instance._container.StartAsync(cancellationTokenSource.Token);
            return instance;
        }

        public static async Task<ISingleServer> NewServerAsync(Pkcs12Store cert)
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var instance = new TestContainerServer();
            instance.WriteCerts();
            await instance._container.StartAsync(cancellationTokenSource.Token);
            return instance;
        }
    }
}