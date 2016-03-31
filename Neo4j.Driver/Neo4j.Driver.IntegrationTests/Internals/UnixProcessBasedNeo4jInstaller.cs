using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;


namespace Neo4j.Driver.IntegrationTests.Internals
{
    /// <summary>
    ///     Unix implementation of the <see cref="INeo4jInstaller"/>.
    /// </summary>
    internal class UnixProcessBasedNeo4jInstaller : INeo4jInstaller
    {
        private Process startedProcess = null;

        public DirectoryInfo Neo4jHome { get; private set; }

        public void DownloadNeo4j()
        {
            Neo4jHome = Neo4jServerFilesDownloadHelper.DownloadNeo4j(
                Neo4jServerEdition.Enterprise,
                Neo4jServerPlatform.Unix,
                new TgzNeo4jServerFileExtractor());

            UpdateSettings(new Dictionary<string, string> { { "dbms.security.auth_enabled", "false" } });// disable auth
        }

        public void InstallServer()
        {
            // Not needed
        }

        public void StartServer()
        {
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = Path.Combine(Neo4jHome.FullName, @"bin/neo4j");
            processInfo.WorkingDirectory = Neo4jHome.FullName;
            processInfo.Arguments = "console";
            processInfo.UseShellExecute = true;
            processInfo.CreateNoWindow = false;

            File.AppendAllLines("log.txt", new[] { processInfo.WorkingDirectory });
            File.AppendAllLines("log.txt", new[] { processInfo.FileName });

            startedProcess = Process.Start(processInfo);

            Task.Delay(15000).Wait();
        }

        public void StopServer()
        {
            if (startedProcess == null) return;
            startedProcess.CloseMainWindow();
            if (!startedProcess.WaitForExit(10000))
            {
                startedProcess.Kill();
                startedProcess.WaitForExit();
            }
            startedProcess = null;
        }

        public void UninstallServer()
        {
            // Not needed
        }

        public void UpdateSettings(IDictionary<string, string> keyValuePair)
        {
            Neo4jSettingsHelper.UpdateSettings(Neo4jHome.FullName, keyValuePair);
        }
    }
}

