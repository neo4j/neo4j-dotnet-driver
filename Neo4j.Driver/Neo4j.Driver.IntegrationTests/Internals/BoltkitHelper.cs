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
using System.IO;
using System.Linq;
using System.Reflection;
using static System.Environment;
using static Neo4j.Driver.Internal.Routing.ServerVersion;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public static class BoltkitHelper
    {
        public static readonly string TargetDir = DiscoverTargetDirectory();

        public const string TestRequireBoltkit = "Test is skipped due to Boltkit not accessible";
        private const string TestRequireEnterprise = "Test is skipped due to enterprise server is not accessible";

        private static readonly string DefaultServerVersion = "3.4.1";
        private static string _boltkitArgs;
        private static BoltkitStatus _boltkitAvailable = BoltkitStatus.Unknown;
        private static Tuple<bool, string> _isClusterSupported;
        private static readonly object _syncLock = new object();

        public static string BoltkitArgs
        {
            get
            {
                if (_boltkitArgs != null)
                {
                    return _boltkitArgs;
                }
                // User could always overwrite the env var
                var envVar = GetEnvironmentVariable("NEOCTRLARGS");
                if (envVar != null)
                {
                    _boltkitArgs = envVar;
                }
                else // If a user did not specify any value then we compute a default one based on if he has access to the enterprise server
                {
                    if (GetEnvironmentVariable("AWS_ACCESS_KEY_ID") != null &&
                        GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") != null)
                    {
                        // enterprise server
                        _boltkitArgs = $"-e {DefaultServerVersion}";
                    }
                    else
                    {
                        // community server
                        _boltkitArgs = DefaultServerVersion;
                    }
                }
                return _boltkitArgs;
            }
        }

        private enum BoltkitStatus
        {
            Unknown, Installed, Unavailable
        }

        public static bool IsBoltkitAvailable()
        {
            if (_boltkitAvailable == BoltkitStatus.Unknown)
            {
                lock (_syncLock)
                {
                    // only update it once
                    if (_boltkitAvailable == BoltkitStatus.Unknown)
                    {
                        _boltkitAvailable = TestBoltkitAvailability();
                    }
                }
            }
            return _boltkitAvailable == BoltkitStatus.Installed;
        }

        public static Tuple<bool, string> IsClusterSupported()
        {
            if (_isClusterSupported != null)
            {
                return _isClusterSupported;
            }

            var supported = true;
            var message = "All good to go";

            if (!IsBoltkitAvailable())
            {
                supported = false;
                message = TestRequireBoltkit;
            }
            else if (!IsEnterprise())
            {
                supported = false;
                message = TestRequireEnterprise;
            }
            else if (!(Version(ServerVersion()) >= V3_1_0))
            {
                supported = false;
                message = $"Server {ServerVersion()} does not support causal cluster";
            }

            _isClusterSupported = new Tuple<bool, string>(supported, message);
            return _isClusterSupported;
        }

        public static string ServerVersion()
        {
            // the last of the args is the version to installed
            var strings = BoltkitArgs.Split(null);
            return strings.Last();
        }

        private static BoltkitStatus TestBoltkitAvailability()
        {
            try
            {
                var commandRunner = ShellCommandRunnerFactory.Create();
                commandRunner.RunCommand("neoctrl-cluster", "--help");
            }
            catch
            {
                return BoltkitStatus.Unavailable;
            }
            return BoltkitStatus.Installed;
        }

        private static bool IsEnterprise()
        {
            var strings = BoltkitArgs.Split(null);
            return strings.Contains("-e");
        }

        private static string DiscoverTargetDirectory()
        {
            var codeBase = typeof(BoltkitHelper).GetTypeInfo().Assembly.CodeBase;
            var localPath = new Uri(codeBase).LocalPath;
            var localFile = new FileInfo(localPath);
            var sourcePath = new DirectoryInfo(Path.Combine(localFile.DirectoryName, string.Format("..{0}..{0}..{0}..{0}..{0}Target", Path.DirectorySeparatorChar)));
            return sourcePath.FullName;
        }
    }
}
