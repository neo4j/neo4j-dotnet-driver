using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Neo4j.Driver.IntegrationTests
{
    public class IntegrationTestFixture : IDisposable
    {
        private readonly Neo4jInstaller _installer = new Neo4jInstaller();
        public IntegrationTestFixture()
        {
//            _installer.DownloadNeo4j().Wait();
//            _installer.InstallServer();
//            _installer.StartServer();
        }
        
        public void Dispose()
        {
//            _installer.StopServer();
//            _installer.UninstallServer();
        }
    }

    [CollectionDefinition(CollectionName)]
    public class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>
    {
        public const string CollectionName = "Integration";
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    public static class Extensions
    {
        public static float BytesToMegabytes(this long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }
    }
}
