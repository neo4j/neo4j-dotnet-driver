// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Linq;
using System.Text;
using Neo4j.Driver.Internal.Types;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.IO.StructHandlers
{
    internal class NodeHandler : IPackStreamStructHandler
    {
        public IEnumerable<byte> ReadableStructs => new[] {PackStream.Node};

        public IEnumerable<Type> WritableTypes => Enumerable.Empty<Type>();

        public object Read(IPackStreamReader reader, byte signature, long size)
        {
            var urn = reader.ReadLong();

            var numLabels = (int) reader.ReadListHeader();
            var labels = new List<string>(numLabels);
            for (var i = 0; i < numLabels; i++)
            {
                labels.Add(reader.ReadString());
            }
            var numProps = (int) reader.ReadMapHeader();
            var props = new Dictionary<string, object>(numProps);
            for (var j = 0; j < numProps; j++)
            {
                var key = reader.ReadString();
                props.Add(key, reader.Read());
            }

            return new Node(urn, labels, props);
        }

        public void Write(IPackStreamWriter writer, object value)
        {
            throw new ProtocolException($"It is not allowed to send Node({string.Join(",", ReadableStructs)}) values to the server.");
        }
    }
}
