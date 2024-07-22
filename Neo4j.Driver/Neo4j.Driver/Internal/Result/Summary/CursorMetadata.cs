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

namespace Neo4j.Driver.Internal.Result;

/// <summary>For passing information from cursor to the summary builder.</summary>
/// <param name="ResultHadRecords"></param>
internal record struct CursorMetadata(bool ResultHadRecords, bool ResultHadKeys)
{
    public IList<IGqlStatusObject> BuildStatusObjects(IList<IGqlStatusObject> builderGqlStatusObjects)
    {
        var length = (builderGqlStatusObjects?.Count ?? 0) + 1;
        var result = new IGqlStatusObject[length];
        for (var i = 1; i < length; i++)
        {
            result[i] = builderGqlStatusObjects[i - 1];
        }

        result[0] = ResultHadRecords switch
        {
            true => GqlStatusObject.Success,
            false when ResultHadKeys => GqlStatusObject.NoData,
            false when !ResultHadKeys => GqlStatusObject.OmittedResult
        };

        return result;
    }
}
