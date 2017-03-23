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
using Neo4j.Driver.Internal;
using static Neo4j.Driver.IntegrationTests.Internals.WindowsPowershellRunner;
using Path = System.IO.Path;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class ExternalBoltkitInstaller : IInstaller
    {

        private static readonly string HomeDir = Path.Combine(BoltkitHelper.TargetDir, "neo4jhome");

        private const string Password = "neo4j";
        private const string HttpUri = "http://localhost:7474";
        private const string BoltUri = "bolt://localhost:7687";

        public void Install()
        {
            if (Directory.Exists(HomeDir))
            {
                Debug($"Found and using server intalled at `{HomeDir}`.");
            }
            else
            {
                var args = new List<string>();
                args.AddRange(BoltkitHelper.BoltkitArgs.Split(null));
                args.Add(BoltkitHelper.TargetDir);
                var tempHomeDir = RunCommand("neoctrl-install", args.ToArray()).Single();
                Debug($"Downloaded server at `{tempHomeDir}`, now renaming to `{HomeDir}`.");

                Directory.Move(tempHomeDir, HomeDir);
                Debug($"Installed server at `{HomeDir}`.");
            }

            RunCommand("neoctrl-create-user", new[] { HomeDir, "neo4j", "neo4j" });
        }

        public ISet<ISingleInstance> Start()
        {
            Debug("Starting server...");
            RunCommand("neoctrl-start", HomeDir);
            Debug("Server started.");
            return new HashSet<ISingleInstance> { new SingleInstance(HttpUri, BoltUri, HomeDir, Password) };
        }

        public void Stop()
        {
            Debug("Stopping server...");
            RunCommand("neoctrl-stop", HomeDir);
            Debug("Server stopped.");
        }

        public void Kill()
        {
            Debug("Killing server...");
            RunCommand("neoctrl-stop", new []{"-k", HomeDir});
            Debug("Server killed.");
        }



        public void UpdateSettings(IDictionary<string, string> keyValuePair)
        {
            Stop();
            Debug($"Updating server config to {keyValuePair.ValueToString()}");
            Neo4jSettingsHelper.UpdateSettings(HomeDir, keyValuePair);
            Start();
        }

        public void EnsureProcedures(string sourceProcedureJarPath)
        {
            var jarName = new DirectoryInfo(sourceProcedureJarPath).Name;

            var pluginFolderPath = Path.Combine(HomeDir, "plugins");
            var destProcedureJarPath = Path.Combine(pluginFolderPath, jarName);

            if (!File.Exists(destProcedureJarPath))
            {
                Stop();
                Debug($"Adding procedure {jarName}");
                File.Copy(sourceProcedureJarPath, destProcedureJarPath);
                Start();
            }
        }
    }
}