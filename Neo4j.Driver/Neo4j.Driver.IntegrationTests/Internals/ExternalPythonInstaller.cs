// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class ExternalPythonInstaller : INeo4jInstaller
    {
        private static readonly string NeorunArgs = "-p neo4j";
        private static readonly string NeorunPath = new DirectoryInfo("../../../../neokit/neorun.py").FullName;
        private static readonly string Neo4jHomePath = new DirectoryInfo("../../../../Target/neo4jhome").FullName;

        public DirectoryInfo Neo4jHome => new DirectoryInfo(Neo4jHomePath);

        public void DownloadNeo4j()
        {
        }

        public void InstallServer()
        {
        }

        public void StartServer()
        {
            var args = new List<string> {NeorunPath, $"--start={Neo4jHome.FullName}"};
            args.AddRange(NeorunArgs.Split(null));
            WindowsPowershellRunner.RunPowershellCommand("python", args.ToArray());
        }

        public void StopServer()
        {
            WindowsPowershellRunner.RunPowershellCommand("python", new[]
            {
                NeorunPath,
                $"--stop={Neo4jHome.FullName}"
            });
        }

        public void UninstallServer()
        {
        }

        public void UpdateSettings(IDictionary<string, string> keyValuePair)
        {
            StopServer();
            Neo4jSettingsHelper.UpdateSettings(Neo4jHome.FullName, keyValuePair);
            StartServer();
        }

        public void EnsureProcedures(string sourceProcedureJarPath)
        {
            var jarName = new DirectoryInfo(sourceProcedureJarPath).Name;

            var pluginFolderPath = Path.Combine(Neo4jHome.FullName, "plugins");
            var destProcedureJarPath = Path.Combine(pluginFolderPath, jarName);

            if (!File.Exists(destProcedureJarPath))
            {
                StopServer();
                File.Copy(sourceProcedureJarPath, destProcedureJarPath);
                StartServer();
            }
        }
    }
}
