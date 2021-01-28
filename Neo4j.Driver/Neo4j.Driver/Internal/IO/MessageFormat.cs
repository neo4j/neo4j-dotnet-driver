﻿// Copyright (c) "Neo4j"
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

namespace Neo4j.Driver.Internal.IO
{
    internal abstract class MessageFormat: IMessageFormat
    {
        private readonly bool _supportBytes;
        private readonly IDictionary<byte, IPackStreamStructHandler> _readerStructHandlers = new Dictionary<byte, IPackStreamStructHandler>();
        private readonly IDictionary<Type, IPackStreamStructHandler> _writerStructHandlers = new Dictionary<Type, IPackStreamStructHandler>();

        protected MessageFormat(bool supportBytes)
        {
            _supportBytes = supportBytes;
        }

        public IPackStreamReader CreateReader(Stream stream)
        {
            return _supportBytes ? new PackStreamReader(stream, _readerStructHandlers) : new PackStreamReaderBytesIncompatible(stream, _readerStructHandlers);
        }

        public IPackStreamWriter CreateWriter(Stream stream)
        {
            if (_supportBytes)
            {
                return new PackStreamWriter(stream, _writerStructHandlers);
            }

            return new PackStreamWriterBytesIncompatible(stream, _writerStructHandlers);
        }

        protected void AddHandler<T>()
            where T : IPackStreamStructHandler, new() 
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
    }
}