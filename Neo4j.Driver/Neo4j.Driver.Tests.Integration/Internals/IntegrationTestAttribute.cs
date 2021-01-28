// Copyright (c) "Neo4j"
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
using System.Text;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Internal.Util;
using Xunit;

namespace Neo4j.Driver.IntegrationTests
{
    public class RequireBoltStubServerFactAttribute : FactAttribute
    {
        public RequireBoltStubServerFactAttribute()
        {
            if (!BoltkitHelper.IsBoltkitAvailable())
            {
                Skip = BoltkitHelper.TestRequireBoltkit;
            }
        }
    }

    public class RequireBoltStubServerTheoryAttribute : TheoryAttribute
    {
        public RequireBoltStubServerTheoryAttribute()
        {
            if (!BoltkitHelper.IsBoltkitAvailable())
            {
                Skip = BoltkitHelper.TestRequireBoltkit;
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
    }


    /// <summary>
    /// Use `RequireServerFact` tag for the tests that require a single instance
    /// </summary>
    public class RequireServerFactAttribute : FactAttribute
    {
        public RequireServerFactAttribute(string versionText = null,
            VersionComparison versionCompare = VersionComparison.EqualTo)
        {
            var skipText = new StringBuilder();

            if (Environment.GetEnvironmentVariable("TEST_NEO4J_USING_TESTKIT") == null  &&  !BoltkitHelper.IsBoltkitAvailable())
            {
                skipText.AppendLine(BoltkitHelper.TestRequireBoltkit);
            }

            if (!string.IsNullOrWhiteSpace(versionText))
            {
                var version = ServerVersion.From(versionText);
                var availableVersion = ServerVersion.From(BoltkitHelper.ServerVersion());
                
                var satisfy = false;
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

            Skip = skipText.ToString();
        }
    }
    
    public class RequireEnterpriseEdition : RequireServerFactAttribute
    {
        public RequireEnterpriseEdition(string versionText = null,
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
    }

    public class RequireServerWithIPv6FactAttribute : RequireServerFactAttribute
    {
        public RequireServerWithIPv6FactAttribute(string versionText = null,
            VersionComparison versionCompare = VersionComparison.EqualTo)
            : base(versionText, versionCompare)
        {
            if (string.IsNullOrEmpty(Skip))
            {
                if (!BoltkitHelper.IPV6Available())
                {
                    Skip = "IPv6 is not available";
                }
                else if (!BoltkitHelper.IPV6Enabled())
                {
                    Skip = "IPv6 is disabled";
                }
            }
        }
    }

    /// <summary>
    /// Use `RequireServerTheory` tag for the tests that require a single instance
    /// </summary>
    public class RequireServerTheoryAttribute : TheoryAttribute
    {
        public RequireServerTheoryAttribute(string versionText = null,
            VersionComparison versionCompare = VersionComparison.EqualTo)
        {
            var skipText = new StringBuilder();

            if (!BoltkitHelper.IsBoltkitAvailable())
            {
                skipText.AppendLine(BoltkitHelper.TestRequireBoltkit);
            }

            if (!string.IsNullOrWhiteSpace(versionText))
            {
                var version = ServerVersion.From(versionText);
                var availableVersion = ServerVersion.From(BoltkitHelper.ServerVersion());

                var satisfy = false;
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
                        $"Test requires available server version {availableVersion} to be {versionCompare} {version}.");
                }

                Skip = skipText.ToString();
            }
        }
    }

    /// <summary>
    /// Use `RequireClusterFact` tag for the tests that require a cluster
    /// </summary>
    public class RequireClusterFactAttribute : FactAttribute
    {
        public RequireClusterFactAttribute(string versionText = null,
            VersionComparison versionCompare = VersionComparison.EqualTo)
        {
            var skipText = new StringBuilder();

            if (!BoltkitHelper.IsBoltkitAvailable())
            {
                skipText.AppendLine(BoltkitHelper.TestRequireBoltkit);
            }

            if (!string.IsNullOrWhiteSpace(versionText))
            {
                var version = ServerVersion.From(versionText);
                var availableVersion = ServerVersion.From(BoltkitHelper.ServerVersion());

                var satisfy = false;
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

            Skip = skipText.ToString();
        }
    }

    /// <summary>
    /// Use `RequireClusterTheory` tag for the tests that require a cluster
    /// </summary>
    public class RequireClusterTheoryAttribute : TheoryAttribute
    {
        public RequireClusterTheoryAttribute()
        {
            var isClusterSupported = BoltkitHelper.IsClusterSupported();
            if (!isClusterSupported.Item1)
            {
                Skip = isClusterSupported.Item2;
            }
        }
    }

	public class ShouldNotRunInTestKitFact : FactAttribute 
    {
        public ShouldNotRunInTestKitFact()
		{
            string envVariable = Environment.GetEnvironmentVariable("TEST_NEO4J_USING_TESTKIT");
            if (!string.IsNullOrEmpty(envVariable)  &&  envVariable.Equals("true", StringComparison.OrdinalIgnoreCase))
			{
                Skip = "Test is not run in TestKit";                
			}
		}
    }

    public class ShouldNotRunInTestKit_RequireServerFactAttribute : RequireServerFactAttribute
	{
        public ShouldNotRunInTestKit_RequireServerFactAttribute()
		{
            string envVariable = Environment.GetEnvironmentVariable("TEST_NEO4J_USING_TESTKIT");
            if (!string.IsNullOrEmpty(envVariable) && envVariable.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                Skip = "Test is not run in TestKit";
            }
        }
    }
}