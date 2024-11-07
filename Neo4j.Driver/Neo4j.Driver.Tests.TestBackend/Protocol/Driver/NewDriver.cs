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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver.Tests.TestBackend.Protocol.Auth;
using Neo4j.Driver.Tests.TestBackend.Resolvers;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Driver;

internal class NewDriver : ProtocolObject
{
    public NewDriverType data { get; set; } = new();

    [JsonIgnore]
    public IDriver Driver { get; set; }

    [JsonIgnore]
    private Controller Control { get; set; }

    public override Task Process(Controller controller)
    {
        Control = controller;

        if (data.authorizationToken != null)
        {
            var authToken = data.authorizationToken.AsToken();

            Driver = GraphDatabase.Driver(data.uri, authToken, DriverConfig);
        }
        else
        {
            var authDataManager = ObjManager.GetObject(data.authTokenManagerId);

            if (authDataManager is NewNeo4jAuthTokenManager atm)
            {
                Driver = GraphDatabase.Driver(data.uri, atm.TokenManager, DriverConfig);
            }
            else
            {
                Driver = GraphDatabase.Driver(data.uri, authDataManager as IAuthTokenManager, DriverConfig);
            }
        }

        return Task.CompletedTask;
    }

    public override string Respond()
    {
        return new ProtocolResponse("Driver", uniqueId).Encode();
    }

    private void DriverConfig(ConfigBuilder configBuilder)
    {
        configBuilder.WithMetricsEnabled(true).WithIpv6Enabled(true);

        if (!string.IsNullOrEmpty(data.userAgent))
        {
            configBuilder.WithUserAgent(data.userAgent);
        }

        if (data.resolverRegistered)
        {
            configBuilder.WithResolver(new ListAddressResolver(Control, data.uri));
        }

        if (data.connectionTimeoutMs > 0)
        {
            configBuilder.WithConnectionTimeout(TimeSpan.FromMilliseconds(data.connectionTimeoutMs));
        }

        if (data.maxConnectionPoolSize.HasValue)
        {
            configBuilder.WithMaxConnectionPoolSize(data.maxConnectionPoolSize.Value);
        }

        if (data.connectionAcquisitionTimeoutMs.HasValue)
        {
            configBuilder.WithConnectionAcquisitionTimeout(
                TimeSpan.FromMilliseconds(data.connectionAcquisitionTimeoutMs.Value));
        }

        if (data.ModifiedTrustedCertificates)
        {
            var certificateTrustStrategy = data?.trustedCertificates switch
            {
                null => CertificateTrustRule.TrustSystem,
                { Length: 0 } => CertificateTrustRule.TrustAny,
                _ => CertificateTrustRule.TrustList
            };

            List<string> GetPaths()
            {
                var env = Environment.GetEnvironmentVariables();
                if (!env.Contains("TK_CUSTOM_CA_PATH"))
                {
                    throw new Exception("Need to define path to custom CAs");
                }

                var path = env["TK_CUSTOM_CA_PATH"].ToString();
                return data?.trustedCertificates?.Select(x => $"{path}{x}").ToList();
            }

            configBuilder.WithCertificateTrustRule(
                certificateTrustStrategy,
                certificateTrustStrategy == CertificateTrustRule.TrustList ? GetPaths() : null);
        }

        if (data.maxTxRetryTimeMs.HasValue)
        {
            configBuilder.WithMaxTransactionRetryTime(TimeSpan.FromMilliseconds(data.maxTxRetryTimeMs.Value));
        }

        if (data.encrypted.HasValue)
        {
            configBuilder.WithEncryptionLevel(
                data.encrypted.Value
                    ? EncryptionLevel.Encrypted
                    : EncryptionLevel.None);
        }

        if (data.fetchSize.HasValue)
        {
            configBuilder.WithFetchSize(data.fetchSize.Value);
        }

        if (data.notificationsMinSeverity != null || data.notificationsDisabledCategories != null)
        {
            if (data.notificationsMinSeverity == TestkitConstants.NotificationsConfig.Disabled)
            {
                configBuilder.WithNotificationsDisabled();
            }
            else
            {
                var sev = Enum.TryParse<Severity>(data.notificationsMinSeverity ?? string.Empty, true, out var severity)
                    ? (Severity?)severity
                    : null;

                var cats = data.notificationsDisabledCategories
                    ?.Select(x => Enum.Parse<Category>(x, true))
                    .ToArray();

                configBuilder.WithNotifications(sev, cats);
            }
        }

        if (data.telemetryDisabled.HasValue && data.telemetryDisabled.Value)
        {
            configBuilder.WithTelemetryDisabled();
        }

        if (data.clientCertificate != null)
        {
            configBuilder.WithClientCertificateProvider(
                ClientCertificateProviders.Static(data.clientCertificate.Certificate));
        }

        if(data.clientCertificateProviderId != null)
        {
            var provider = (NewClientCertificateProvider)ObjManager.GetObject(data.clientCertificateProviderId);
            configBuilder.WithClientCertificateProvider(provider);
        }

        if (data.livenessCheckTimeoutMs.HasValue)
        {
            configBuilder.WithConnectionLivenessCheckTimeout(
                TimeSpan.FromMilliseconds(data.livenessCheckTimeoutMs.Value));
        }

        var logger = new SimpleLogger();
        configBuilder.WithLogger(logger);
    }

    [JsonConverter(typeof(NewDriverConverter))]
    public class NewDriverType
    {
        // * NOTE: this class is not automatically deserialized, it is done by the NewDriverConverter class
        // so if you add a new property here, make sure to add it to the NewDriverConverter class as well

        private string[] _trustedCertificates = {};

        public long? fetchSize;
        public long? maxTxRetryTimeMs;

        [JsonIgnore]
        public bool ModifiedTrustedCertificates;

        public string uri { get; set; }
        public AuthorizationToken authorizationToken { get; set; } = new();
        public string authTokenManagerId { get; set; } = "";
        public string userAgent { get; set; }
        public bool resolverRegistered { get; set; } = false;
        public bool domainNameResolverRegistered { get; set; } = false;
        public int connectionTimeoutMs { get; set; } = -1;
        public int? maxConnectionPoolSize { get; set; }
        public int? connectionAcquisitionTimeoutMs { get; set; }
        public bool? telemetryDisabled { get; set; }
        public ClientCertificate clientCertificate { get; set; }
        public string clientCertificateProviderId { get; set; }

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

        public string notificationsMinSeverity { get; set; }
        public string[] notificationsDisabledCategories { get; set; }
        public int? livenessCheckTimeoutMs { get; set; }
    }
}
