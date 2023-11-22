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
using System.Linq;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata;

internal class BoltPatchCollector : IMetadataCollector<string[]>
{
    public const string BoltPatchKey = "patch_bolt";
    object IMetadataCollector.Collected => Collected;

    public string[] Collected { get; private set; }

    public void Collect(IDictionary<string, object> metadata)
    {
        if (metadata == null || !metadata.TryGetValue(BoltPatchKey, out var value))
        {
            return;
        }

        if (value is List<object> values)
        {
            Collected = values.OfType<string>().ToArray();
        }
        else
        {
            throw new ProtocolException(
                $"Expected '{BoltPatchKey}' metadata to be of type 'List<object>', but got '{value?.GetType().Name}'.");
        }
    }
}
