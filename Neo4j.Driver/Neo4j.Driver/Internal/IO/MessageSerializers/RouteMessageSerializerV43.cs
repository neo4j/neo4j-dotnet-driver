// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using System.Collections.Generic;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO.MessageSerializers;

internal class RouteMessageSerializerV43 : WriteOnlySerializer
{
    internal static RouteMessageSerializerV43 Instance = new();

    private static readonly Type[] Types = { typeof(RouteMessageV43) };
    public override IEnumerable<Type> WritableTypes => Types;

    public override void Serialize(PackStreamWriter writer, object value)
    {
        if (value is not RouteMessageV43 msg)
        {
            throw new ArgumentOutOfRangeException(
                $"Encountered {value?.GetType().Name} where {nameof(RouteMessageV43)} was expected");
        }

        writer.WriteStructHeader(3, MessageFormat.MsgRoute);
        writer.WriteDictionary(msg.Routing);
        writer.WriteList(msg.Bookmarks.Values);
        writer.WriteString(string.IsNullOrEmpty(msg.DatabaseParam) ? null : msg.DatabaseParam);
    }
}
