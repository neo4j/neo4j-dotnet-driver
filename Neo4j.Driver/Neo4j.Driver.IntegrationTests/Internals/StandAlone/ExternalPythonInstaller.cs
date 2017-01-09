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

using System.Collections.Generic;
using System.IO;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class ExternalPythonInstaller : IInstaller
    {
        private const string NeorunArgs = "-p neo4j";
        private static readonly string NeorunPath = new DirectoryInfo("../../../../neokit/neorun.py").FullName;
        private static readonly string HomePath = new DirectoryInfo("../../../../Target/neo4jhome").FullName;

        private const string Password = "neo4j";
        private const string HttpUri = "http://localhost:7474";
        private const string BoltUri = "bolt://localhost:7687";


        public void Install()
        {
        }

        public ISet<ISingleInstance> Start()
        {
            var args = new List<string> {NeorunPath, $"--start={HomePath}"};
            args.AddRange(NeorunArgs.Split(null));
            WindowsPowershellRunner.RunPythonCommand(args.ToArray());
            return new HashSet<ISingleInstance> {new SingleInstance(HttpUri, BoltUri, HomePath, Password)};
        }

        public void Stop()
        {
            WindowsPowershellRunner.RunPythonCommand(new[]
            {
                NeorunPath,
                $"--stop={HomePath}"
            });
        }

        public void Kill()
        {
            Stop();
        }

        public void UpdateSettings(IDictionary<string, string> keyValuePair)
        {
            Stop();
            Neo4jSettingsHelper.UpdateSettings(HomePath, keyValuePair);
            Start();
        }

        public void EnsureProcedures(string sourceProcedureJarPath)
        {
            var jarName = new DirectoryInfo(sourceProcedureJarPath).Name;

            var pluginFolderPath = Path.Combine(HomePath, "plugins");
            var destProcedureJarPath = Path.Combine(pluginFolderPath, jarName);

            if (!File.Exists(destProcedureJarPath))
            {
                Stop();
                File.Copy(sourceProcedureJarPath, destProcedureJarPath);
                Start();
            }
        }
    }
}
