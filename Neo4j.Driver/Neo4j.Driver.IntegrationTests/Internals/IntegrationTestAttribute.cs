// Copyright (c) 2002-2018 "Neo Technology,"
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

using Xunit;
using static Neo4j.Driver.IntegrationTests.Internals.BoltkitHelper;
using static Neo4j.Driver.Internal.Routing.ServerVersion;

namespace Neo4j.Driver.IntegrationTests
{
    public class RequireBoltStubServerFactAttribute : FactAttribute
    {
        public RequireBoltStubServerFactAttribute()
        {
            if (!IsBoltkitAvailable())
            {
                Skip = TestRequireBoltkit;
            }
        }
    }

    /// <summary>
    /// Use `RequireServerVersionGreaterThanOrEqualToFact` tag for the tests that require a server with version equals to or greater than given version
    /// </summary>
    public class RequireServerVersionGreaterThanOrEqualToFact : FactAttribute
    {
        public RequireServerVersionGreaterThanOrEqualToFact(string version)
        {
            if (!IsBoltkitAvailable())
            {
                Skip = TestRequireBoltkit;
            }
            if (!(Version(ServerVersion()) >= Version(version)))
            {
                Skip = $"Require server version >= {version}, while current server version is {ServerVersion()}";
            }
        }
    }

    /// <summary>
    /// Use `RequireServerVersionLessThanFact` tag for the tests that require a server with version less than the given version
    /// </summary>
    public class RequireServerVersionLessThanFact : FactAttribute
    {
        public RequireServerVersionLessThanFact(string version)
        {
            if (!IsBoltkitAvailable())
            {
                Skip = TestRequireBoltkit;
            }
            if (Version(ServerVersion()) >= Version(version))
            {
                Skip = $"Require server version < {version}, while current server version is {ServerVersion()}";
            }
        }
    }

    /// <summary>
    /// Use `RequireServerFact` tag for the tests that require a single instance
    /// </summary>
    public class RequireServerFactAttribute : FactAttribute
    {
        public RequireServerFactAttribute()
        {
            if (!IsBoltkitAvailable())
            {
                Skip = TestRequireBoltkit;
            }
        }
    }

    /// <summary>
    /// Use `RequireServerTheory` tag for the tests that require a single instance
    /// </summary>
    public class RequireServerTheoryAttribute : TheoryAttribute
    {
        public RequireServerTheoryAttribute()
        {
            if (!IsBoltkitAvailable())
            {
                Skip = TestRequireBoltkit;
            }
        }
    }

    /// <summary>
    /// Use `RequireClusterFact` tag for the tests that require a cluster
    /// </summary>
    public class RequireClusterFactAttribute : FactAttribute
    {
        public RequireClusterFactAttribute()
        {
            var isClusterSupported = IsClusterSupported();
            if (!isClusterSupported.Item1)
            {
                Skip = isClusterSupported.Item2;
            }
        }
    }

    /// <summary>
    /// Use `RequireClusterTheory` tag for the tests that require a cluster
    /// </summary>
    public class RequireClusterTheoryAttribute : TheoryAttribute
    {
        public RequireClusterTheoryAttribute()
        {
            var isClusterSupported = IsClusterSupported();
            if (!isClusterSupported.Item1)
            {
                Skip = isClusterSupported.Item2;
            }
        }
    }
}
