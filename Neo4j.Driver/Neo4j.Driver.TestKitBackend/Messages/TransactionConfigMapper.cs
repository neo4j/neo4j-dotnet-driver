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

using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Types;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal interface ITransactionConfigMapper
{
    Action<TransactionConfigBuilder> Map(Dictionary<string, ICypherValue>? txMeta, Optional<long?> timeout);
}

internal class TransactionConfigMapper : ITransactionConfigMapper
{
    private readonly ICypherToNativeMapper _cypherToNativeMapper;

    public TransactionConfigMapper(ICypherToNativeMapper cypherToNativeMapper)
    {
        _cypherToNativeMapper = cypherToNativeMapper;
    }

    public Action<TransactionConfigBuilder> Map(Dictionary<string, ICypherValue>? txMeta, Optional<long?> timeout)
    {
        return builder =>
        {
            if (txMeta is not null)
            {
                builder.WithMetadata(_cypherToNativeMapper.Map(txMeta));
            }

            if (timeout.IsSpecified(out var timeoutMs) && timeoutMs is { } ms)
            {
                builder.WithTimeout(TimeSpan.FromMilliseconds(ms));
            }
        };
    }
}
