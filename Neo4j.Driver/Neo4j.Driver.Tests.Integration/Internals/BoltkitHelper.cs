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
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using static System.Environment;

namespace Neo4j.Driver.IntegrationTests.Internals;

public static class BoltkitHelper
{
    public const string TestRequireBoltkit = "Test is skipped due to Boltkit not accessible";
    private const string TestRequireEnterprise = "Test is skipped due to enterprise server is not accessible";
    public static readonly string TargetDir = DiscoverTargetDirectory();

    private static readonly string DefaultServerVersion = "4.0";
    private static string _boltkitArgs;
    private static BoltkitStatus _boltkitAvailable = BoltkitStatus.Unknown;
    private static Tuple<bool, string> _isClusterSupported;
    private static readonly object SyncLock = new();

    public static string BoltkitArgs
    {
        get
        {
            if (_boltkitArgs != null)
            {
                return _boltkitArgs;
            }

            // User could always overwrite the env var
            var envVar = GetEnvironmentVariable("NEOCTRL_ARGS");
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

    private static bool IsBoltkitAvailable()
    {
        if (_boltkitAvailable == BoltkitStatus.Unknown)
        {
            lock (SyncLock)
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

    public static bool StubServerAvailable()
    {
        return IsBoltkitAvailable();
    }

    public static bool ServerAvailable()
    {
        return !string.IsNullOrEmpty(GetEnvironmentVariable("TEST_NEO4J_USING_TESTKIT")) || IsBoltkitAvailable();
    }

    public static Tuple<bool, string> IsClusterSupported()
    {
        if (_isClusterSupported != null)
        {
            return _isClusterSupported;
        }

        var supported = true;
        var message = "All good to go";

        if (!ServerAvailable())
        {
            supported = false;
            message = TestRequireBoltkit;
        }
        else if (!IsEnterprise())
        {
            supported = false;
            message = TestRequireEnterprise;
        }

        _isClusterSupported = new Tuple<bool, string>(supported, message);
        return _isClusterSupported;
    }

    public static bool Ipv6Available()
    {
        return NetworkInterface.GetAllNetworkInterfaces().Any(nic => nic.Supports(NetworkInterfaceComponent.IPv6));
    }

    public static bool Ipv6Enabled()
    {
        return !bool.TryParse(GetEnvironmentVariable("NEOCTRL_DISABLE_IPV6"), out var disableIPv6) || !disableIPv6;
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

    public static bool IsEnterprise()
    {
        var strings = BoltkitArgs.Split(null);
        return strings.Contains("-e");
    }

    private static string DiscoverTargetDirectory()
    {
        var codeBase = typeof(BoltkitHelper).GetTypeInfo().Assembly.Location;
        var localPath = new Uri(codeBase).LocalPath;
        var localFile = new FileInfo(localPath);
        var sourcePath = new DirectoryInfo(
            Path.Combine(
                localFile.DirectoryName!,
                string.Format("..{0}..{0}..{0}..{0}..{0}Target", Path.DirectorySeparatorChar)));

        return sourcePath.FullName;
    }

    private enum BoltkitStatus
    {
        Unknown,
        Installed,
        Unavailable
    }
}
