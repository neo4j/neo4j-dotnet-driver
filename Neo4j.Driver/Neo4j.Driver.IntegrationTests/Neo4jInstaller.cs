using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Threading.Tasks;
using Microsoft.PowerShell;

namespace Neo4j.Driver.IntegrationTests
{
    public class Neo4jInstaller
    {
        private static string Version => Environment.GetEnvironmentVariable("version") ?? "3.0.0-NIGHTLY";
        private static string PackageUrl => $"http://alpha.neohq.net/dist/neo4j-enterprise-{Version}-windows.zip";
        private const string ServiceName = "neo4j-driver-test-server";

        private static DirectoryInfo Neo4jDir => new DirectoryInfo("../target/neo4j");
        private static DirectoryInfo Neo4jHomeDir => new DirectoryInfo(Path.Combine(Neo4jDir.FullName, $"/{Version}"));


        private void EnsureDirectoriesExist()
        {
            if (!Neo4jDir.Exists)
                Neo4jDir.Create();

            if (!Neo4jHomeDir.Exists)
                Neo4jHomeDir.Create();
        }

        public async Task DownloadNeo4j()
        {
            EnsureDirectoriesExist();

            var downloadFileInfo = new FileInfo($"../target/{Version}.zip");
            if (downloadFileInfo.Directory != null)
            {
                if (!downloadFileInfo.Directory.Exists)
                    downloadFileInfo.Directory.Create();
            }

            long expectedSize;
            using (var client = new WebClient())
            {

                client.OpenRead(PackageUrl);
                expectedSize = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                if (!downloadFileInfo.Exists || downloadFileInfo.Length != expectedSize)
                {
                    client.DownloadProgressChanged += (s, e) => { Console.Write("."); };
                    await client.DownloadFileTaskAsync(PackageUrl, downloadFileInfo.FullName);
                }
            }

            downloadFileInfo.Refresh();
            if (!downloadFileInfo.Exists)
                throw new IOException($"Unable to download the server from {PackageUrl}");
            if (downloadFileInfo.Length != expectedSize)
                throw new IOException($"File at {PackageUrl} was downloaded, but it's size {expectedSize.BytesToMegabytes()}Mb doesn't match the size expected {expectedSize.BytesToMegabytes()}Mb");

            Neo4jDir.Refresh();
            if (!Neo4jDir.Exists || Neo4jDir.GetDirectories().Length == 0)
            {
                ExtractZip(downloadFileInfo.FullName);
            }

            LoadPowershellModule();
        }

        private Runspace _runspace;
        private void LoadPowershellModule()
        {
            var moduleLocation = @"C:\Users\x1\Documents\GitHub\neo4j-dotnet-driver\Neo4j.Driver\Neo4j.Driver.IntegrationTests\bin\target\neo4j\neo4j-enterprise-3.0.0-M03\bin\Neo4j-Management\Neo4j-Management.psm1";

            InitialSessionState initial = InitialSessionState.CreateDefault();
            initial.ExecutionPolicy = ExecutionPolicy.RemoteSigned;
            initial.ImportPSModule(new[] { moduleLocation });
            _runspace = RunspaceFactory.CreateRunspace(initial);
            _runspace.Open();
        }

        private static void ExtractZip(string filename)
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(filename, Neo4jDir.FullName);
        }


        public void InstallServer()
        {
            RunPowershellCommand("Install-Neo4jServer", "Name");
        }

        public void UninstallServer()
        {
            RunPowershellCommand("Uninstall-Neo4jServer");
        }

        public void StartServer()
        {
            RunPowershellCommand("Start-Neo4jServer");
        }

        public void StopServer()
        {
            RunPowershellCommand("Stop-Neo4jServer");
        }

        private void RunPowershellCommand(string command, string serviceNameParam = "ServiceName")
        {
            using (var powershell = PowerShell.Create())
            {
                powershell.Runspace = _runspace;
                powershell.AddCommand(command);
                powershell.AddParameter("Neo4jServer", Neo4jHomeDir.FullName);
                powershell.AddParameter(serviceNameParam, ServiceName);
                powershell.Invoke();
                
            }
        }
    }
}