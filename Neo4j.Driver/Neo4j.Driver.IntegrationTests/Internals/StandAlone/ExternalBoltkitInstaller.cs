// Copyright (c) 2002-2018 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
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
using Neo4j.Driver.Internal.Routing;
using static Neo4j.Driver.IntegrationTests.Internals.Neo4jSettingsHelper;
using Path = System.IO.Path;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class ExternalBoltkitInstaller : IInstaller
    {

        private static readonly string HomeDir = Path.Combine(BoltkitHelper.TargetDir, "neo4jhome");

        private const string Password = "neo4j";
        private const string HttpUri = "http://localhost:7474";
        private const string BoltUri = "bolt://localhost:7687";

        private readonly IShellCommandRunner _commandRunner;

        public ExternalBoltkitInstaller()
        {
            _commandRunner = ShellCommandRunnerFactory.Create();
        }

        public void Install()
        {
            if (Directory.Exists(HomeDir))
            {
                _commandRunner.Debug($"Found and using server installed at `{HomeDir}`.");
            }
            else
            {
                var args = new List<string>();
                args.AddRange(BoltkitHelper.BoltkitArgs.Split(null));
                args.Add($"\"{BoltkitHelper.TargetDir}\"");
                var tempHomeDir = _commandRunner.RunCommand("neoctrl-install", args.ToArray()).Single();
                _commandRunner.Debug($"Downloaded server at `{tempHomeDir}`, now renaming to `{HomeDir}`.");

                Directory.Move(tempHomeDir, HomeDir);
                _commandRunner.Debug($"Installed server at `{HomeDir}`.");
            }

            _commandRunner.RunCommand("neoctrl-create-user", $"\"{HomeDir}\"", "neo4j", Password);
            UpdateSettings(new Dictionary<string, string>
            {
                {ListenAddr, Ipv6EnabledAddr}
            });

            // This is added because current default for `dbms.connector.bolt.thread_pool_max_size` is `400`
            // which is lower than Driver's default max pool size setting of `500`. This is added because
            // soak tests were failing
            // TODO: Remove/Revise after 3.4.0 config defaults are finalised.
            if (ServerVersion.Version(BoltkitHelper.ServerVersion()) >= ServerVersion.V3_4_0)
            {
                UpdateSettings(new Dictionary<string, string>
                {
                    {MaxThreadPoolSize, Pool500}
                });
            }
        }

        public ISet<ISingleInstance> Start()
        {
            _commandRunner.Debug("Starting server...");
            _commandRunner.RunCommand("neoctrl-start", $"\"{HomeDir}\"");
            _commandRunner.Debug("Server started.");
            return new HashSet<ISingleInstance> { new SingleInstance(HttpUri, BoltUri, HomeDir, Password) };
        }

        public void Stop()
        {
            _commandRunner.Debug("Stopping server...");
            _commandRunner.RunCommand("neoctrl-stop", $"\"{HomeDir}\"");
            _commandRunner.Debug("Server stopped.");
        }

        public void Kill()
        {
            _commandRunner.Debug("Killing server...");
            _commandRunner.RunCommand("neoctrl-stop", "-k", $"\"{HomeDir}\"");
            _commandRunner.Debug("Server killed.");
        }

        public void EnsureRunningWithSettings(IDictionary<string, string> keyValuePair)
        {
            Stop();
            UpdateSettings(keyValuePair);
            Start();
        }

        private void UpdateSettings(IDictionary<string, string> keyValuePair)
        {
            _commandRunner.Debug($"Updating server config to {keyValuePair.ToContentString()}");
            Neo4jSettingsHelper.UpdateSettings(HomeDir, keyValuePair);
        }

        public void EnsureProcedures(string sourceProcedureJarPath)
        {
            var jarName = new DirectoryInfo(sourceProcedureJarPath).Name;

            var pluginFolderPath = Path.Combine(HomeDir, "plugins");
            var destProcedureJarPath = Path.Combine(pluginFolderPath, jarName);

            if (!File.Exists(destProcedureJarPath))
            {
                Stop();
                _commandRunner.Debug($"Adding procedure {jarName}");
                File.Copy(sourceProcedureJarPath, destProcedureJarPath);
                Start();
            }
        }
    }
}
