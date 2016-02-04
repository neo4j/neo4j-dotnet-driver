//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Threading.Tasks;
using Microsoft.PowerShell;
using Neo4j.Driver.Exceptions;

namespace Neo4j.Driver.IntegrationTests
{
    public class Neo4jInstaller
    {
        private static string Version => Environment.GetEnvironmentVariable("version") ?? "3.0.0-NIGHTLY";
//        private static string PackageUrl => $"http://alpha.neohq.net/dist/neo4j-enterprise-{Version}-windows.zip";
        private static string PackageUrl => $"http://alpha.neohq.net/dist/neo4j-community-{Version}-windows.zip";
        private const string ServiceName = "neo4j-driver-test-server";

        private static DirectoryInfo Neo4jDir => new DirectoryInfo("../target/neo4j");
        private DirectoryInfo _extractedLocation;

        private void EnsureDirectoriesExist()
        {
            if (!Neo4jDir.Exists)
                Neo4jDir.Create();
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

            bool downloadedNew = false;
            long expectedSize;
            using (var client = new WebClient())
            {

                client.OpenRead(PackageUrl);
                expectedSize = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                if (!downloadFileInfo.Exists || downloadFileInfo.Length != expectedSize)
                {
                    client.DownloadProgressChanged += (s, e) => { Console.Write("."); };
                    await client.DownloadFileTaskAsync(PackageUrl, downloadFileInfo.FullName);
                    downloadedNew = true;
                }
            }

            downloadFileInfo.Refresh();
            if (!downloadFileInfo.Exists)
                throw new IOException($"Unable to download the server from {PackageUrl}");
            if (downloadFileInfo.Length != expectedSize)
                throw new IOException($"File at {PackageUrl} was downloaded, but it's size {expectedSize.BytesToMegabytes()}Mb doesn't match the size expected {expectedSize.BytesToMegabytes()}Mb");

            Neo4jDir.Refresh();
            var zipFolder = GetZipFolder(downloadFileInfo.FullName);

            var extractedPath = Path.Combine(Neo4jDir.FullName, zipFolder);

            if (Directory.Exists(extractedPath))
            {
                var dirs = Neo4jDir.GetDirectories(zipFolder);
                if (dirs.Length != 0)
                {
                    var extractedDir = new DirectoryInfo(Path.Combine(Neo4jDir.FullName, zipFolder));
                    if (downloadedNew)
                    {
                        extractedDir.Delete(true);
                        ExtractZip(downloadFileInfo.FullName);
                    }
                }
            }
            else
            {
                ExtractZip(downloadFileInfo.FullName);
            }

            _extractedLocation = new DirectoryInfo(Path.Combine(Neo4jDir.FullName, zipFolder));
            LoadPowershellModule(_extractedLocation.FullName);
        }

        private static string GetZipFolder(string filename)
        {
            using (var archive = ZipFile.OpenRead(filename))
            {
                return archive.Entries.Where(a => a.Name == string.Empty).OrderBy(a => a.FullName.Length).First().FullName;
            }
        }

        private Runspace _runspace;
        private void LoadPowershellModule(string extractedLocation)
        {
            var moduleLocation = Path.Combine(extractedLocation, "bin\\Neo4j-Management\\Neo4j-Management.psm1");

            InitialSessionState initial = InitialSessionState.CreateDefault();
#if ! BUILDSERVER
            initial.ExecutionPolicy = ExecutionPolicy.RemoteSigned;
#endif
            initial.ImportPSModule(new[] { moduleLocation });
            _runspace = RunspaceFactory.CreateRunspace(initial);
            _runspace.Open();

        }

        private static void ExtractZip(string filename)
        {
            ZipFile.ExtractToDirectory(filename, Neo4jDir.FullName);
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
                powershell.AddParameter("Neo4jServer", _extractedLocation.FullName);
                powershell.AddParameter(serviceNameParam, ServiceName);
                powershell.Invoke();
                if (powershell.HadErrors)
                {
                    throw new Neo4jException("Integration", CollectAsString(powershell.Streams.Error));
                }
            }
        }

        private string CollectAsString(PSDataCollection<ErrorRecord> errors)
        {
            var output = errors.Select(error => error.ToString()).ToList();
            return string.Join(Environment.NewLine, output);
        }
    }
}