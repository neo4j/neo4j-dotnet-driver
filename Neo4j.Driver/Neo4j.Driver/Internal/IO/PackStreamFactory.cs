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

using System.IO;
using Neo4j.Driver.Internal.Connector;

namespace Neo4j.Driver.Internal.IO;

internal interface IPackStreamFactory
{
    PackStreamWriter BuildWriter(MessageFormat format, IChunkWriter stream);
    PackStreamReader BuildReader(MessageFormat format, MemoryStream stream, ByteBuffers buffers);
}

internal sealed class PackStreamFactory : IPackStreamFactory
{
    internal static readonly PackStreamFactory Default = new();

    private PackStreamFactory()
    {
    }

    public PackStreamWriter BuildWriter(MessageFormat format, IChunkWriter stream)
    {
        return new PackStreamWriter(format, stream.Stream);
    }

    public PackStreamReader BuildReader(MessageFormat format, MemoryStream stream, ByteBuffers buffers)
    {
        return new PackStreamReader(format, stream, buffers);
    }
}
