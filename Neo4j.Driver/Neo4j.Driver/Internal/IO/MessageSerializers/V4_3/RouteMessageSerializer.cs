// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Neo4j.Driver.Internal.Messaging.V4_3;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.IO.MessageSerializers.V4_3;

internal class RouteMessageSerializer : WriteOnlySerializer
{
    public override IEnumerable<Type> WritableTypes => new[] {typeof(RouteMessage)};

    public override void Serialize(PackStreamWriter writer, object value)
    {
        var msg = value.CastOrThrow<RouteMessage>();

        writer.WriteStructHeader(3, MessageFormat.MsgRoute);
        writer.Write(msg.Routing);
        writer.Write(msg.Bookmarks.Values);
        writer.Write(string.IsNullOrEmpty(msg.DatabaseParam) ? null : msg.DatabaseParam);
    }
}