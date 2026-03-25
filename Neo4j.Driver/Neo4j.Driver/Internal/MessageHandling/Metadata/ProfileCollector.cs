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
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata;

internal class ProfileCollector : IMetadataCollector<IProfile>
{
    internal const string ProfileKey = "profile";

    object IMetadataCollector.Collected => Collected;

    public IProfile Collected { get; private set; }

    public void Collect(IDictionary<string, object> metadata)
    {
        if (metadata != null && metadata.TryGetValue(ProfileKey, out var profileValue))
        {
            switch (profileValue)
            {
                case null:
                    Collected = null;
                    break;

                case IDictionary<string, object> profileDict:
                    Collected = CollectProfile(profileDict);
                    break;

                default:
                    throw new ProtocolException(
                        $"Expected '{ProfileKey}' metadata to be of type 'IDictionary<String,Object>', but got '{profileValue?.GetType().Name}'.");
            }
        }
    }

    private static IProfile CollectProfile(IDictionary<string, object> profileDictionary)
    {
        if (profileDictionary.Count == 0)
        {
            return null;
        }

        var operationType =
            profileDictionary.GetMandatoryValue<string>("operatorType", m => new ProtocolException(m));

        var args = profileDictionary.GetValue("args", new Dictionary<string, object>());
        var identifiers = profileDictionary.GetValue("identifiers", new List<object>()).Cast<string>();
        var dbHits = TryGetOrNull<long>(profileDictionary, "dbHits");
        var rows = TryGetOrNull<long>(profileDictionary, "rows");
        var pageCacheHits = TryGetOrNull<long>(profileDictionary, "pageCacheHits");
        var pageCacheMisses = TryGetOrNull<long>(profileDictionary, "pageCacheMisses");
        var pageCacheHitRatio = TryGetOrNull<double>(profileDictionary, "pageCacheHitRatio");
        var time = TryGetOrNull<long>(profileDictionary, "time");

        var children = profileDictionary.GetValue("children", new List<object>());

        var childPlans = children
            .Select(child => child as IDictionary<string, object>)
            .Select(CollectProfile)
            .Where(childProfile => childProfile != null)
            .ToList();

        return new Profile(
            operationType,
            args,
            identifiers.ToList(),
            childPlans,
            dbHits,
            rows,
            pageCacheHits,
            pageCacheMisses,
            pageCacheHitRatio,
            time);
    }

    private static T? TryGetOrNull<T> (IDictionary<string, object> profileDictionary, string key) where T : struct
    {
        if (profileDictionary.TryGetValue<T>(key, default, out var value))
        {
            return value;
        }
        return null;
    }
}
