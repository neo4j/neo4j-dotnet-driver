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

using Neo4j.Driver.IntegrationTests.Internals;
using Xunit;

namespace Neo4j.Driver.IntegrationTests
{
    /// <summary>
    /// Use `RequireServerFact` tag for the tests that requires a single instance
    /// </summary>
    public class RequireServerFactAttribute : FactAttribute
    {
        public RequireServerFactAttribute()
        {
            if (!BoltkitHelper.IsAvaliable())
            {
                Skip = BoltkitHelper.TestRequireBoltkit;
            }
        }
    }

    /// <summary>
    /// Use `RequireClusterFact` tag for the tests that requires a cluster
    /// </summary>
    public class RequireClusterFactAttribute : FactAttribute
    {
        public RequireClusterFactAttribute()
        {
            if (!BoltkitHelper.IsAvaliable())
            {
                Skip = BoltkitHelper.TestRequireBoltkit;
            }
            if (!(ServerVersion.Version(BoltkitHelper.ServerVersion()) >= ServerVersion.V3_1_0))
            {
                Skip = $"Server {BoltkitHelper.ServerVersion()} does not support causal cluster";
            }
        }
    }

    /// <summary>
    /// Use `RequireClusterTheory` tag for the tests that requires a cluster
    /// </summary>
    public class RequireClusterTheoryAttribute : TheoryAttribute
    {
        public RequireClusterTheoryAttribute()
        {
            if (!BoltkitHelper.IsAvaliable())
            {
                Skip = BoltkitHelper.TestRequireBoltkit;
            }
            if (!(ServerVersion.Version(BoltkitHelper.ServerVersion()) >= ServerVersion.V3_1_0))
            {
                Skip = $"Server {BoltkitHelper.ServerVersion()} does not support causal cluster";
            }
        }
    }
}