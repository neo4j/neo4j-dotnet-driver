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

using System.Diagnostics.CodeAnalysis;
using Neo4j.Driver.TestKitBackend.Cypher;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal interface ISubtestSkipPolicy
{
    bool TryGetSkipReason(
        string testName,
        IReadOnlyDictionary<string, ICypherValue> arguments,
        [NotNullWhen(true)] out string? reason);
}

internal class SubtestSkipPolicy : ISubtestSkipPolicy
{
    private static readonly string[] SkipUnmappableArgumentsFragments = ["test_zoned_time"];

    private readonly ICypherToNativeMapper _cypherToNativeMapper;

    public SubtestSkipPolicy(ICypherToNativeMapper cypherToNativeMapper)
    {
        _cypherToNativeMapper = cypherToNativeMapper;
    }

    public bool TryGetSkipReason(
        string testName,
        IReadOnlyDictionary<string, ICypherValue> arguments,
        [NotNullWhen(true)] out string? reason)
    {
        reason = SkipsUnmappableArguments(testName)
            ? arguments.Values.Select(MappingFailure).FirstOrDefault(failure => failure != null)
            : null;

        return reason != null;
    }

    private bool SkipsUnmappableArguments(string testName)
    {
        return SkipUnmappableArgumentsFragments.Any(
            fragment => testName.Contains(fragment, StringComparison.Ordinal));
    }

    private string? MappingFailure(ICypherValue value)
    {
        try
        {
            _cypherToNativeMapper.Map(value);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
