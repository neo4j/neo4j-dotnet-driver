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

namespace Neo4j.Driver.TestKitBackend.Messages;

// Substring-matched, like the legacy backend's TestBlackList - entries get added here as parity
// work uncovers tests this backend genuinely can't pass yet, not copied wholesale from the legacy
// list (most of which papers over gaps this backend doesn't share).
internal class SubstringSkipPolicy : ISkipPolicy
{
    private static readonly (string Fragment, string Reason)[] Entries = [];

    public bool TryGetSkipReason(string testName, out string reason)
    {
        foreach (var entry in Entries)
        {
            if (testName.Contains(entry.Fragment, StringComparison.Ordinal))
            {
                reason = entry.Reason;
                return true;
            }
        }

        reason = "";
        return false;
    }
}
