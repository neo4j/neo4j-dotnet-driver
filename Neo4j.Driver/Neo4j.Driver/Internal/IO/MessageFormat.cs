// Copyright (c) 2002-2020 "Neo4j,"
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
using System.IO;
using System.Linq;

namespace Neo4j.Driver.Internal.IO
{
    internal abstract class MessageFormat : IMessageFormat
    {
        private readonly IDictionary<byte, IPackStreamSerializer> _readerStructHandlers =
            new Dictionary<byte, IPackStreamSerializer>();

        private readonly IDictionary<Type, IPackStreamSerializer> _writerStructHandlers =
            new Dictionary<Type, IPackStreamSerializer>();

        protected MessageFormat()
        {
        }

        public IPackStreamReader CreateReader(Stream stream)
        {
            return new PackStreamReader(stream, _readerStructHandlers);
        }

        public IPackStreamWriter CreateWriter(Stream stream)
        {
            return new PackStreamWriter(stream, _writerStructHandlers);
        }

        protected void AddHandler<T>()
            where T : IPackStreamSerializer, new()
        {
            var handler = new T();

            foreach (var readableStruct in handler.ReadableStructs)
            {
                _readerStructHandlers.Add(readableStruct, handler);
            }

            foreach (var writableType in handler.WritableTypes)
            {
                _writerStructHandlers.Add(writableType, handler);
            }
        }

        protected void RemoveHandler<T>()
        {
            _readerStructHandlers.Where(kvp => kvp.Value is T).Select(kvp => kvp.Key).ToList()
                .ForEach(b => _readerStructHandlers.Remove(b));
            _writerStructHandlers.Where(kvp => kvp.Value is T).Select(kvp => kvp.Key).ToList()
                .ForEach(t => _writerStructHandlers.Remove(t));
        }
    }
}