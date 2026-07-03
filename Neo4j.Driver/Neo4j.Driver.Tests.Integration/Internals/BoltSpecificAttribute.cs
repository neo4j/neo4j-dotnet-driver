// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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
using System.Reflection;
using Xunit;
using Xunit.v3;

namespace Neo4j.Driver.IntegrationTests.Internals;

/// <summary>
/// Marks a test (or an entire test class) as exercising Bolt-specific behaviour that has no Query
/// API equivalent — e.g. routing, reactive sessions, or TLS/certificate handling. Such tests are
/// skipped when the suite runs over the Query API protocol (<c>TEST_NEO4J_PROTOCOL=queryapi</c>)
/// and run normally over Bolt. Composes with the existing <c>[RequireServerFact]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class BoltSpecificAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (DefaultInstallation.UseQueryApi)
        {
            Assert.Skip("Bolt-specific test; not applicable to the Query API protocol.");
        }
    }
}
