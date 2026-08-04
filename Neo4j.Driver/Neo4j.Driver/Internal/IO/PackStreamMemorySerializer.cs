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

#nullable enable

using System;
using System.IO;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.IO;

[DriverAutoRegister(singleton: true)]
internal class PackStreamMemorySerializer : IPackStreamMemorySerializer
{
    private readonly IPackStreamReaderWriterFactory _readerWriterFactory;

    public PackStreamMemorySerializer(IPackStreamReaderWriterFactory readerWriterFactory)
    {
        _readerWriterFactory = readerWriterFactory;
    }

    public byte[] Serialize(MessageFormat format, Action<IPackStreamWriter> write)
    {
        using var stream = new MemoryStream();
        var writer = _readerWriterFactory.CreateWriter(format, stream);
        write(writer);
        return stream.ToArray();
    }

    public T Deserialize<T>(MessageFormat format, byte[] bytes, Func<IPackStreamReader, T> read)
    {
        return read(_readerWriterFactory.CreateReader(format, new MemoryStream(bytes)));
    }
}
