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
using Microsoft.PowerShell;
using Neo4j.Driver.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;

namespace Neo4j.Driver.IntegrationTests.Internals
{
  public class WindowsServiceBasedNeo4jInstaller : INeo4jInstaller
    {
        public DirectoryInfo Neo4jHome { get; private set; }

        public void DownloadNeo4j()
        {
            Neo4jHome = Neo4jServerFilesDownloadHelper.DownloadNeo4j(
              Neo4jServerEdition.Enterprise,
              Neo4jServerPlatform.Windows,
              new ZipNeo4jServerFileExtractor());

            UpdateSettings(new Dictionary<string, string>{ { "dbms.security.auth_enabled", "false"} });// disable auth
            LoadPowershellModule(Neo4jHome.FullName);
        }

        public void UpdateSettings(IDictionary<string, string> keyValuePair)
        {
          Neo4jSettingsHelper.UpdateSettings(Neo4jHome.FullName, keyValuePair);
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
// seems have some problem to work with powershell 4.0
#if ! BUILDSERVER 
                if (powershell.HadErrors)
                {
                    throw new Neo4jException("Integration", CollectAsString(powershell.Streams.Error));
                }
#endif
            }
        }

        private string CollectAsString(PSDataCollection<ErrorRecord> errors)
        {
            var output = errors.Select(error => error.ToString()).ToList();
            return string.Join(Environment.NewLine, output);
        }
    }
}
