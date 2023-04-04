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
using System.Text;
using Neo4j.Driver.Internal.Util;
using Xunit;

namespace Neo4j.Driver.IntegrationTests.Internals;

public sealed class RequireBoltStubServerFactAttribute : FactAttribute
{
    //Default server version required to run stub tests is anything less than 4.3. After this version testkit takes over the stub tests.
    public RequireBoltStubServerFactAttribute(
        string versionText = "4.3.0",
        VersionComparison versionCompare = VersionComparison.LessThan)
    {
        var skipText = new StringBuilder();

        CheckStubServer(skipText);

        RequireServer.RequiredServerAvailable(versionText, versionCompare, skipText);

        if (skipText.Length > 0)
        {
            Skip = skipText.ToString();
        }
    }

    private void CheckStubServer(StringBuilder skipText)
    {
        if (!BoltkitHelper.StubServerAvailable())
        {
            skipText.Append(BoltkitHelper.TestRequireBoltkit);
        }
    }
}

public sealed class RequireBoltStubServerTheoryAttribute : TheoryAttribute
{
    //Default server version required to run stub tests is anything less than 4.3. After this version testkit takes over the stub tests.
    public RequireBoltStubServerTheoryAttribute(
        string versionText = "4.3.0",
        VersionComparison versionCompare = VersionComparison.LessThan)
    {
        var skipText = new StringBuilder();

        CheckStubServer(skipText);
        RequireServer.RequiredServerAvailable(versionText, versionCompare, skipText);

        if (skipText.Length > 0)
        {
            Skip = skipText.ToString();
        }
    }

    private void CheckStubServer(StringBuilder skipText)
    {
        if (!BoltkitHelper.StubServerAvailable())
        {
            skipText.Append(BoltkitHelper.TestRequireBoltkit);
        }
    }
}

public enum VersionComparison
{
    LessThan,
    LessThanOrEqualTo,
    EqualTo,
    GreaterThanOrEqualTo,
    GreaterThan,
    Between
}

public static class RequireServer
{
    public static bool RequiredServerAvailable(
        string versionText,
        VersionComparison versionCompare,
        StringBuilder skipText)
    {
        var satisfy = true;

        if (!string.IsNullOrWhiteSpace(versionText))
        {
            var version = ServerVersion.From(versionText);
            var availableVersion = ServerVersion.From(BoltkitHelper.ServerVersion());

            switch (versionCompare)
            {
                case VersionComparison.LessThan:
                    satisfy = availableVersion.CompareTo(version) < 0;
                    break;

                case VersionComparison.LessThanOrEqualTo:
                    satisfy = availableVersion.CompareTo(version) <= 0;
                    break;

                case VersionComparison.EqualTo:
                    satisfy = availableVersion.CompareTo(version) == 0;
                    break;

                case VersionComparison.GreaterThanOrEqualTo:
                    satisfy = availableVersion.CompareTo(version) >= 0;
                    break;

                case VersionComparison.GreaterThan:
                    satisfy = availableVersion.CompareTo(version) > 0;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(versionCompare));
            }

            if (!satisfy)
            {
                skipText.AppendLine(
                    $"Test requires available server version {availableVersion} to be {versionCompare.ToString()} {version}.");
            }
        }

        return satisfy;
    }

    public static bool RequiredServerAvailable(string versionText, VersionComparison versionCompare)
    {
        var satisfy = true;

        if (!string.IsNullOrWhiteSpace(versionText))
        {
            var version = ServerVersion.From(versionText);
            var availableVersion = ServerVersion.From(BoltkitHelper.ServerVersion());

            switch (versionCompare)
            {
                case VersionComparison.LessThan:
                    satisfy = availableVersion.CompareTo(version) < 0;
                    break;

                case VersionComparison.LessThanOrEqualTo:
                    satisfy = availableVersion.CompareTo(version) <= 0;
                    break;

                case VersionComparison.EqualTo:
                    satisfy = availableVersion.CompareTo(version) == 0;
                    break;

                case VersionComparison.GreaterThanOrEqualTo:
                    satisfy = availableVersion.CompareTo(version) >= 0;
                    break;

                case VersionComparison.GreaterThan:
                    satisfy = availableVersion.CompareTo(version) > 0;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(versionCompare));
            }
        }

        return satisfy;
    }

    public static bool RequiredServerAvailableBetween(
        string minVersionText,
        string maxVersionText,
        StringBuilder skipText)
    {
        var minVersion = ServerVersion.From(minVersionText);
        var maxVersion = ServerVersion.From(maxVersionText);
        var availableVersion = ServerVersion.From(BoltkitHelper.ServerVersion());

        var satisfy = availableVersion >= minVersion && availableVersion < maxVersion;

        if (!satisfy)
        {
            skipText.AppendLine(
                $"Test requires available server version {availableVersion} to be between {minVersion} and {maxVersion}.");
        }

        return satisfy;
    }
}

/// <summary>Use `RequireServerFact` tag for the tests that require a single instance</summary>
public class RequireServerFactAttribute : FactAttribute
{
    private readonly VersionComparison _versionComparison;

    public RequireServerFactAttribute(
        string versionText = null,
        VersionComparison versionCompare = VersionComparison.EqualTo)
    {
        var skipText = new StringBuilder();

        if (!BoltkitHelper.ServerAvailable())
        {
            skipText.AppendLine(BoltkitHelper.TestRequireBoltkit);
        }

        RequireServer.RequiredServerAvailable(versionText, versionCompare, skipText);

        if (skipText.Length > 0)
        {
            Skip = skipText.ToString();
        }
    }

    public RequireServerFactAttribute(
        string minVersionText,
        string maxVersionText,
        VersionComparison versionComparison)
    {
        if (versionComparison != VersionComparison.Between)
            throw new ArgumentException(nameof(versionComparison));

        _versionComparison = versionComparison;
        var skipText = new StringBuilder();

        if (!BoltkitHelper.ServerAvailable())
        {
            skipText.AppendLine(BoltkitHelper.TestRequireBoltkit);
        }

        RequireServer.RequiredServerAvailableBetween(minVersionText, maxVersionText, skipText);

        if (skipText.Length > 0)
        {
            Skip = skipText.ToString();
        }
    }
}

public sealed class RequireEnterpriseEdition : RequireServerFactAttribute
{
    public RequireEnterpriseEdition(
        string versionText = null,
        VersionComparison versionCompare = VersionComparison.EqualTo)
        : base(versionText, versionCompare)
    {
        if (string.IsNullOrEmpty(Skip))
        {
            if (!BoltkitHelper.IsEnterprise())
            {
                Skip = "Test requires Neo4j enterprise edition.";
            }
        }
    }

    public RequireEnterpriseEdition(
        string minVersionText,
        string maxVersionText,
        VersionComparison versionComparison) : base(minVersionText, maxVersionText, versionComparison)
    {
        if (string.IsNullOrEmpty(Skip))
        {
            if (!BoltkitHelper.IsEnterprise())
            {
                Skip = "Test requires Neo4j enterprise edition.";
            }
        }
    }
}

public sealed class RequireServerWithIPv6FactAttribute : RequireServerFactAttribute
{
    public RequireServerWithIPv6FactAttribute(
        string versionText = null,
        VersionComparison versionCompare = VersionComparison.EqualTo)
        : base(versionText, versionCompare)
    {
        if (string.IsNullOrEmpty(Skip))
        {
            if (!BoltkitHelper.Ipv6Available())
            {
                Skip = "IPv6 is not available";
            }
            else if (!BoltkitHelper.Ipv6Enabled())
            {
                Skip = "IPv6 is disabled";
            }
        }
    }
}

/// <summary>Use `RequireServerTheory` tag for the tests that require a single instance</summary>
public sealed class RequireServerTheoryAttribute : TheoryAttribute
{
    public RequireServerTheoryAttribute(
        string versionText = null,
        VersionComparison versionCompare = VersionComparison.EqualTo)
    {
        var skipText = new StringBuilder();

        if (!BoltkitHelper.ServerAvailable())
        {
            skipText.AppendLine(BoltkitHelper.TestRequireBoltkit);
        }

        RequireServer.RequiredServerAvailable(versionText, versionCompare, skipText);

        if (skipText.Length > 0)
        {
            Skip = skipText.ToString();
        }
    }
}

/// <summary>Use `RequireClusterFact` tag for the tests that require a cluster</summary>
public sealed class RequireClusterFactAttribute : FactAttribute
{
    public RequireClusterFactAttribute(
        string versionText = null,
        VersionComparison versionCompare = VersionComparison.EqualTo)
    {
        var skipText = new StringBuilder();

        if (!BoltkitHelper.ServerAvailable())
        {
            skipText.AppendLine(BoltkitHelper.TestRequireBoltkit);
        }

        RequireServer.RequiredServerAvailable(versionText, versionCompare, skipText);

        if (skipText.Length > 0)
        {
            Skip = skipText.ToString();
        }
    }
}

public sealed class ShouldNotRunInTestKitFact : FactAttribute
{
    public ShouldNotRunInTestKitFact()
    {
        var envVariable = Environment.GetEnvironmentVariable("TEST_NEO4J_USING_TESTKIT");
        if (!string.IsNullOrEmpty(envVariable) && envVariable.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Test is not run in TestKit";
        }
    }
}

public sealed class ShouldNotRunInTestKitRequireServerFactAttribute : RequireServerFactAttribute
{
    public ShouldNotRunInTestKitRequireServerFactAttribute()
    {
        var envVariable = Environment.GetEnvironmentVariable("TEST_NEO4J_USING_TESTKIT");
        if (!string.IsNullOrEmpty(envVariable) && envVariable.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Test is not run in TestKit";
        }
    }
}
