﻿// Copyright (c) "Neo4j"
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

using System;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO.MessageSerializers;

internal sealed class RunWithMetadataMessageSerializer : WriteOnlySerializer
{
    internal static RunWithMetadataMessageSerializer Instance = new();

    private static readonly Type[] Types = { typeof(RunWithMetadataMessage) };
    public override IEnumerable<Type> WritableTypes => Types;

    public override void Serialize(PackStreamWriter writer, object value)
    {
        if (value is not RunWithMetadataMessage msg)
        {
            throw new ArgumentOutOfRangeException(
                $"Encountered {value?.GetType().Name} where {nameof(RunWithMetadataMessage)} was expected");
        }

        writer.WriteStructHeader(3, MessageFormat.MsgRun);
        writer.WriteString(msg.Query.Text);
        writer.WriteDictionary(msg.Query.Parameters);
        writer.WriteDictionary(msg.Metadata);
    }
}
