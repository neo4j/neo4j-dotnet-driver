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

using System.Collections.Generic;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Skip;

internal sealed class TestSkipPolicyRegistry
{
    private static readonly ITestSkipPolicy RunAll = new RunAllPolicy();

    private readonly IReadOnlyList<(string NameFragment, ITestSkipPolicy Policy)> _entries;

    public TestSkipPolicyRegistry(IReadOnlyList<(string NameFragment, ITestSkipPolicy Policy)> entries)
    {
        _entries = entries;
    }

    public ITestSkipPolicy GetPolicy(string testName)
    {
        foreach (var (fragment, policy) in _entries)
        {
            if (testName.Contains(fragment))
            {
                return policy;
            }
        }

        return RunAll;
    }
}
