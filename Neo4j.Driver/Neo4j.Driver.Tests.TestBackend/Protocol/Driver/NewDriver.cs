﻿// Copyright (c) 2002-2022 "Neo4j,"
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
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class NewDriver : ProtocolObject
{
    public NewDriverType data { get; set; } = new();
    [JsonIgnore] public IDriver Driver { get; set; }
    [JsonIgnore] private Controller Control { get; set; }

    public override async Task ProcessAsync(Controller controller)
    {
        Control = controller;
        var authTokenData = data.authorizationToken.data;

        var authToken = authTokenData.scheme switch
        {
            "bearer" => AuthTokens.Bearer(authTokenData.credentials),
            "kerberos" => AuthTokens.Kerberos(authTokenData.credentials),
            _ => AuthTokens.Custom(authTokenData.principal, authTokenData.credentials, authTokenData.realm,
                authTokenData.scheme, authTokenData.parameters)
        };

        Driver = GraphDatabase.Driver(data.uri, authToken, DriverConfig);

        await Task.CompletedTask;
    }

    public override Task ReactiveProcessAsync(Controller controller)
    {
        return ProcessAsync(controller);
    }

    public override string Respond()
    {
        return new ProtocolResponse("Driver", UniqueId).Encode();
    }

    private void DriverConfig(ConfigBuilder configBuilder)
    {
        if (!string.IsNullOrEmpty(data.userAgent))
            configBuilder.WithUserAgent(data.userAgent);

        if (data.resolverRegistered)
            configBuilder.WithResolver(new ListAddressResolver(Control, data.uri));

        if (data.connectionTimeoutMs > 0)
            configBuilder.WithConnectionTimeout(TimeSpan.FromMilliseconds(data.connectionTimeoutMs));

        if (data.maxConnectionPoolSize.HasValue)
            configBuilder.WithMaxConnectionPoolSize(data.maxConnectionPoolSize.Value);

        if (data.connectionAcquisitionTimeoutMs.HasValue)
            configBuilder.WithConnectionAcquisitionTimeout(
                TimeSpan.FromMilliseconds(data.connectionAcquisitionTimeoutMs.Value));

        if (data.ModifiedTrustedCertificates)
        {
            var certificateTrustStrategy = data?.trustedCertificates switch
            {
                null => CertificateTrustRule.TrustSystem,
                {Length: 0} => CertificateTrustRule.TrustAny,
                _ => CertificateTrustRule.TrustList
            };

            List<string> GetPaths()
            {
                var env = Environment.GetEnvironmentVariables();
                if (!env.Contains("TK_CUSTOM_CA_PATH"))
                    throw new Exception("Need to define path to custom CAs");
                var path = env["TK_CUSTOM_CA_PATH"].ToString();
                return data?.trustedCertificates?.Select(x => $"{path}{x}").ToList();
            }

            configBuilder.WithCertificateTrustRule(certificateTrustStrategy,
                certificateTrustStrategy == CertificateTrustRule.TrustList ? GetPaths() : null);
        }

        if (data.maxTxRetryTimeMs.HasValue)
            configBuilder.WithMaxTransactionRetryTime(
                TimeSpan.FromMilliseconds(data.maxTxRetryTimeMs.Value));

        if (data.encrypted.HasValue)
            configBuilder.WithEncryptionLevel(data.encrypted.Value
                ? EncryptionLevel.Encrypted
                : EncryptionLevel.None);

        if (data.fetchSize.HasValue)
            configBuilder.WithFetchSize(data.fetchSize.Value);

        var logger = new SimpleLogger();

        configBuilder.WithLogger(logger);
    }

    [JsonConverter(typeof(NewDriverConverter))]
    public class NewDriverType
    {
        private string[] _trustedCertificates = { };

        public long? fetchSize;
        public long? maxTxRetryTimeMs;
        [JsonIgnore] public bool ModifiedTrustedCertificates;
        public string uri { get; set; }
        public AuthorizationToken authorizationToken { get; set; } = new();
        public string userAgent { get; set; }
        public bool resolverRegistered { get; set; } = false;
        public bool domainNameResolverRegistered { get; set; } = false;
        public int connectionTimeoutMs { get; set; } = -1;
        public int? maxConnectionPoolSize { get; set; }
        public int? connectionAcquisitionTimeoutMs { get; set; }

        public string[] trustedCertificates
        {
            get => _trustedCertificates;
            set
            {
                ModifiedTrustedCertificates = true;
                _trustedCertificates = value;
            }
        }

        public bool? encrypted { get; set; }
    }
}