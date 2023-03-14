// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.IntegrationTests.Internals;

public sealed class ExternalBoltkitClusterInstaller : IInstaller
{
    private const int Cores = 3;
    //TODO: Add read replicas into the cluster too
    //private const int ReadReplicas = 2;

    private const string Password = "cluster";
    private static readonly string ClusterDir = Path.Combine(BoltkitHelper.TargetDir, "cluster");
    private readonly IShellCommandRunner _commandRunner;

    public ExternalBoltkitClusterInstaller()
    {
        _commandRunner = ShellCommandRunnerFactory.Create();
    }

    public void Install()
    {
        if (Directory.Exists(ClusterDir))
        {
            _commandRunner.Debug($"Found and using cluster installed at `{ClusterDir}`.");
            // no need to re-download and change the password if already downloaded locally
            return;
        }

        _commandRunner.RunCommand(
            "neoctrl-cluster",
            "install",
            "--cores",
            $"{Cores}",
            "--password",
            Password,
            BoltkitHelper.ServerVersion(),
            ClusterDir);

        _commandRunner.Debug($"Installed cluster at `{ClusterDir}`.");
    }

    public ISet<ISingleInstance> Start()
    {
        _commandRunner.Debug("Starting cluster...");
        var ret = ParseClusterMember(_commandRunner.RunCommand("neoctrl-cluster", "start", ClusterDir));
        _commandRunner.Debug("Cluster started.");
        return ret;
    }

    public void Stop()
    {
        _commandRunner.Debug("Stopping cluster...");
        _commandRunner.RunCommand("neoctrl-cluster", "stop", ClusterDir);
        _commandRunner.Debug("Cluster stopped.");
    }

    public void Kill()
    {
        _commandRunner.Debug("Killing cluster...");
        _commandRunner.RunCommand("neoctrl-cluster", "stop", "--kill", ClusterDir);
        _commandRunner.Debug("Cluster killed.");
    }

    private ISet<ISingleInstance> ParseClusterMember(string[] lines)
    {
        var members = new HashSet<ISingleInstance>();
        foreach (var line in lines)
        {
            if (line.Trim().Equals(string.Empty))
            {
                // ignore empty lines in the output
                continue;
            }

            var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 3)
            {
                throw new ArgumentException(
                    "Failed to parse cluster member created by boltkit. " +
                    "Expected output to have 'http_uri, bolt_uri, path' in each line. " +
                    $"The output:{Environment.NewLine}{string.Join(Environment.NewLine, lines)}" +
                    $"{Environment.NewLine}The error found in line: {line}");
            }

            members.Add(new SingleInstance(tokens[0], tokens[1], tokens[2], Password));
        }

        return members;
    }
}
