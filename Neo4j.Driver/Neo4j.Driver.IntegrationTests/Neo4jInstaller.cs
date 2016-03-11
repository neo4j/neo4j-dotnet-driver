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

        private static DirectoryInfo Neo4jDir => new DirectoryInfo("../target/neo4j");
        public DirectoryInfo Neo4jHome { get; private set; }

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

            Neo4jHome = new DirectoryInfo(Path.Combine(Neo4jDir.FullName, zipFolder));

            UpdateSettings(new Dictionary<string, string>{ { "dbms.security.auth_enabled", "false"} });// disable auth
            LoadPowershellModule(Neo4jHome.FullName);
        }

        private static string GetZipFolder(string filename)
        {
            using (var archive = ZipFile.OpenRead(filename))
            {
                return archive.Entries.Where(a => a.Name == string.Empty).OrderBy(a => a.FullName.Length).First().FullName;
            }
        }

        private static void ExtractZip(string filename)
        {
            ZipFile.ExtractToDirectory(filename, Neo4jDir.FullName);
        }

        public void UpdateSettings(IDictionary<string, string> keyValuePair)
        {
            UpdateSettings(Neo4jHome.FullName, keyValuePair);
        }

        private static void UpdateSettings(string extractedLocation, IDictionary<string, string> keyValuePair)
        {
            var keyValuePairCopy = new Dictionary<string, string>(keyValuePair);

            // rename the old file to a temp file
            var configFileName = Path.Combine(extractedLocation, "conf/neo4j.conf");
            var tempFileName = Path.Combine(extractedLocation, "conf/neo4j.conf.tmp");
            File.Move(configFileName, tempFileName);

            using (var reader = new StreamReader(tempFileName))
            using (var writer = new StreamWriter(configFileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Trim() == string.Empty || line.Trim().StartsWith("#"))
                    {
                        // empty or comments, print as original
                        writer.WriteLine(line);
                    }
                    else
                    {
                        string[] tokens = line.Split('=');
                        if (tokens.Length == 2 && keyValuePairCopy.ContainsKey(tokens[0].Trim()))
                        {
                            var key = tokens[0].Trim();
                            // found property and update it to the new value
                            writer.WriteLine($"{key}={keyValuePairCopy[key]}");
                            keyValuePairCopy.Remove(key);

                        }
                        else
                        {
                            // not the property that we are looking for, print it as original
                            writer.WriteLine(line);
                        }
                    }
                }

                // write the extral propertes at the end of the file
                foreach (var pair in keyValuePairCopy)
                {
                    writer.WriteLine($"{pair.Key}={pair.Value}");
                }
            }
            // delete the temp file
            File.Delete(tempFileName);
        }

        private Runspace _runspace;

        private void LoadPowershellModule(string extractedLocation)
        {
            var moduleLocation = Path.Combine(extractedLocation, "bin\\Neo4j-Management.psd1");

            InitialSessionState initial = InitialSessionState.CreateDefault();
#if ! BUILDSERVER
            initial.ExecutionPolicy = ExecutionPolicy.RemoteSigned;
#endif
            initial.ImportPSModule(new[] { moduleLocation });
            _runspace = RunspaceFactory.CreateRunspace(initial);
            _runspace.Open();

        }

        public void InstallServer()
        {
            RunPowershellCommand("install-service");
        }

        public void UninstallServer()
        {
            RunPowershellCommand("uninstall-service");
        }

        public void StartServer()
        {
            RunPowershellCommand("start");
            Task.Delay(10000);
        }

        public void StopServer()
        {
            RunPowershellCommand("stop");
        }

        private void RunPowershellCommand(string command)
        {
            using (var powershell = PowerShell.Create())
            {
                powershell.Runspace = _runspace;
                powershell.AddCommand("Invoke-Neo4j");
                powershell.AddArgument(command);
                powershell.Invoke();
                // mute this erorr until the powershell error problem got solved.
//                if (powershell.HadErrors)
//                {
//                    throw new Neo4jException("Integration", CollectAsString(powershell.Streams.Error));
//                }
            }
        }

        private string CollectAsString(PSDataCollection<ErrorRecord> errors)
        {
            var output = errors.Select(error => error.ToString()).ToList();
            return string.Join(Environment.NewLine, output);
        }
    }
}