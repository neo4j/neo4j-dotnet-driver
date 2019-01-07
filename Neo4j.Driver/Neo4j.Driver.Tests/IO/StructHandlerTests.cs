// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Linq;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Tests.IO.Utils;

namespace Neo4j.Driver.Tests.IO
{
    public abstract class StructHandlerTests
    {
        internal abstract IPackStreamStructHandler HandlerUnderTest { get; }

        internal virtual IEnumerable<IPackStreamStructHandler> HandlersNeeded =>
            Enumerable.Empty<IPackStreamStructHandler>();

        protected virtual PackStreamWriterMachine CreateWriterMachine()
        {
            var writerHandlersDict = HandlersNeeded.Union(new[] {HandlerUnderTest}).SelectMany(h => h.WritableTypes,
                    (handler, type) => new KeyValuePair<Type, IPackStreamStructHandler>(type, handler))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return new PackStreamWriterMachine(stream =>
                new PackStreamWriter(stream, writerHandlersDict));
        }

        protected virtual PackStreamReaderMachine CreateReaderMachine(byte[] bytes)
        {
            var readerHandlersDict = HandlersNeeded.Union(new[] { HandlerUnderTest }).SelectMany(h => h.ReadableStructs,
                    (handler, signature) => new KeyValuePair<byte, IPackStreamStructHandler>(signature, handler))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return new PackStreamReaderMachine(bytes, stream =>
                new PackStreamReader(stream, readerHandlersDict));
        }


    }
}
