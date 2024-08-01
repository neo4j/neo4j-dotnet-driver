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

internal sealed record GqlStatusObjectsAndNotifications(
    IList<INotification> Notifications,
    IList<IGqlStatusObject> GqlStatusObjects,
    bool UseRawStatuses)
{
    public IList<IGqlStatusObject> FinalizeStatusObjects(CursorMetadata cursorMetadata)
    {
        if (UseRawStatuses)
        {
            return GqlStatusObjects ?? [];
        }

        return (GqlStatusObjects ?? [])
            .Append(
                cursorMetadata.ResultHadRecords switch
                {
                    true => GqlStatusObject.Success,
                    false when cursorMetadata.ResultHadKeys => GqlStatusObject.NoData,
                    _ => GqlStatusObject.OmittedResult
                })
            //`GQL` spec requires the objects follows the precedence:
            // * A “no data” (`02xxx`) has precedence over a warning;
            // * A warning (`01xxx`) has precedence over a success.
            // * A success (`00xxx`) has precedence over anything informational (`03xxx`)
            .OrderBy(
                x => GetStatusOrderingValue(x) switch
                {
                    "02" => 0,
                    "01" => 1,
                    "00" => 2,
                    "03" => 3,
                    _ => int.MaxValue
                })
            .ToList();
    }

    private static string GetStatusOrderingValue(IGqlStatusObject x)
    {
        var status = x.GqlStatus;
        return status?.Length > 1 ? status.Substring(0, 2) : "";
    }

    public IList<INotification> FinalizeNotifications(CursorMetadata cursorMetadata)
    {
        return Notifications;
    }
}
