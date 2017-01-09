using System;
using System.Collections.Generic;
using System.IO;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class ExternalPythonClusterInstaller : IInstaller
    {
        public DirectoryInfo ClusterPath => new DirectoryInfo("../../../../Target/cluster");
        private const int Cores = 3;
        //TODO Add readreplicas into the cluster too
//        private const int ReadReplicas = 2;

        private const string Password = "cluster";
        // TODO: the version should be read via a system var.
        private const string Neo4jVersion = "3.1.0";

        public bool IsBoltkitAvaliable()
        {
            try
            {
                WindowsPowershellRunner.RunCommand("neoctrl-cluster", "--help");
            }
            catch
            {
                return false;
            }
            return true;
        }

        public void Install()
        {

            if (Directory.Exists(ClusterPath.FullName))
            {
                // no need to redownload and change the password if already downloaded locally
                return;
            }

            WindowsPowershellRunner.RunCommand("neoctrl-cluster", new[] {
                "install",
                "--cores", $"{Cores}", //"--read-replicas", $"{ReadReplicas}", TODO
                "--password", Password,
                Neo4jVersion, ClusterPath.FullName});
        }

        public ISet<ISingleInstance> Start()
        {
            return ParseClusterMember(
                WindowsPowershellRunner.RunCommand("neoctrl-cluster", new[] { "start", ClusterPath.FullName}));
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
                var tokens = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 3)
                {
                    throw new ArgumentException(
                        "Failed to parse cluster memeber created by boltkit. " +
                        "Expected output to have 'http_uri, bolt_uri, path' in each line. " +
                        $"The output:{Environment.NewLine}{string.Join(Environment.NewLine, lines)}" +
                        $"{Environment.NewLine}The error found in line: {line}");
                }
                members.Add(new SingleInstance(tokens[0], tokens[1], tokens[2], Password));
            }
            return members;
        }

        public void Stop()
        {
            WindowsPowershellRunner.RunCommand("neoctrl-cluster", new []{ "stop", ClusterPath.FullName});
        }

        public void Kill()
        {
            WindowsPowershellRunner.RunCommand("neoctrl-cluster", new []{ "stop", "--kill", ClusterPath.FullName});
        }
    }
}
