using System;
using Neo4j.Driver;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class NewDriver : IProtocolObject
    {
        public NewDriverType data { get; set; } = new NewDriverType();
        [JsonIgnore]
        public IDriver Driver { get; set; }

        public class NewDriverType
        {
            public string uri { get; set; }
            public AuthorizationToken authorizationToken { get; set; } = new AuthorizationToken();
            public string userAgent { get; set; }
        }

        void DriverConfig(ConfigBuilder configBuilder)
        {
            if (!string.IsNullOrEmpty(data.userAgent)) configBuilder.WithUserAgent(data.userAgent);
        }

        public override async Task Process()
        {   
            var authTokenData = data.authorizationToken.data;
            var authToken = AuthTokens.Custom(authTokenData.principal, authTokenData.credentials, authTokenData.realm, authTokenData.scheme);

            Driver = GraphDatabase.Driver(data.uri, authToken, DriverConfig);
            
            await Task.CompletedTask;
        }

        public override string Respond()
        {
            return new ProtocolResponse("Driver", uniqueId).Encode();
        }
    }
}
