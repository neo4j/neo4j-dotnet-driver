using System;
using System.IO;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class SingleInstance : ISingleInstance
    {
        public Uri HttpUri { get; }
        public Uri BoltUri { get; }
        public Uri BoltRoutingUri { get; }
        public string HomePath { get; }
        public IAuthToken AuthToken { get; }

        private const string BoltRoutingScheme = "bolt+routing://";
        private const string Username = "neo4j";

        public SingleInstance(string httpUri, string boltUri, string homePath, string password)
        {
            HttpUri = new Uri(httpUri);
            BoltUri = new Uri(boltUri);
            BoltRoutingUri = new Uri(BoltRoutingScheme + $"{BoltUri.Host}:{BoltUri.Port}");
            HomePath = new DirectoryInfo(homePath).FullName;
            AuthToken = AuthTokens.Basic(Username, password);
        }

        public override string ToString()
        {
            return $"Server at endpoint '{HttpUri}', with bolt enabled at endpoint '{BoltUri}', and home path '{HomePath}'.";
        }
    }
}