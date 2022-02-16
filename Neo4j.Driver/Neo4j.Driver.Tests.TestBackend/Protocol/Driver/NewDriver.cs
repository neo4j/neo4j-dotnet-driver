using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class NewDriver : IProtocolObject
    {
        public NewDriverType data { get; set; } = new NewDriverType();
        [JsonIgnore]
        public IDriver Driver { get; set; }
        [JsonIgnore]
        private Controller Control { get; set; }

        [JsonConverter(typeof(NewDriverConverter))]
        public class NewDriverType
        {
            private string[] _trustedCertificates = new string[]{};
            public string uri { get; set; }
            public AuthorizationToken authorizationToken { get; set; } = new AuthorizationToken();
            public string userAgent { get; set; }
            public bool resolverRegistered { get; set; } = false;
            public bool domainNameResolverRegistered { get; set; } = false;
            public int connectionTimeoutMs { get; set; } = -1;
            public int? maxConnectionPoolSize { get; set; }
            public int? connectionAcquisitionTimeoutMs { get; set; }
            [JsonIgnore]
            public bool ModifiedTrustedCertificates = false;

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

        public override async Task Process(Controller controller)
        {
            Control = controller;
            var authTokenData = data.authorizationToken.data;

            IAuthToken authToken;

            switch (authTokenData.scheme)
            {
                case "bearer":
                    authToken = AuthTokens.Bearer(authTokenData.credentials);
                    break;
                case "kerberos":
                    authToken = AuthTokens.Kerberos(authTokenData.credentials);
                    break;

                default:
                    authToken = AuthTokens.Custom(authTokenData.principal,
                                                  authTokenData.credentials,
                                                  authTokenData.realm,
                                                  authTokenData.scheme,
                                                  authTokenData.parameters);
                    break;

            }

            Driver = GraphDatabase.Driver(data.uri, authToken, DriverConfig);

            await Task.CompletedTask;
        }

        public override string Respond()
        {
            return new ProtocolResponse("Driver", uniqueId).Encode();
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
                var path = Environment.GetEnvironmentVariable("TK_CUSTOM_CA_PATH");
                if (path == null)
                    throw new Exception("Need to define path to custom CAs");

                var certificateTrustStrategy = data?.trustedCertificates switch
                {
                    null => CertificateTrustRule.TrustSystem,
                    { Length: 0 } => CertificateTrustRule.TrustAny,
                    _ => CertificateTrustRule.TrustList
                };
                configBuilder.WithCertificateTrustRule(certificateTrustStrategy, data?.trustedCertificates?.Select(x => $"{path}{x}").ToList());
            }

            if (data.encrypted.HasValue)
                configBuilder.WithEncrypted(data.encrypted.Value);

            var logger = new SimpleLogger();

            configBuilder.WithLogger(logger);
        }
    }
}
