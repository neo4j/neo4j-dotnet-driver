// Copyright (c) 2002-2018 "Neo4j,"
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
using System.Text;

namespace Neo4j.Driver.Internal
{
    internal static class StreamExtensions
    {
        public static void Write(this Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static int Read(this Stream stream, byte[] bytes)
        {
            int hasRead = 0, offset = 0, toRead = bytes.Length;
            do
            {
                hasRead = stream.Read(bytes, offset, toRead);
                offset += hasRead;
                toRead -= hasRead;
            } while (toRead > 0 && hasRead > 0);

            if (hasRead <= 0)
            {
                throw new IOException($"Failed to read more from input stream. Expected {bytes.Length} bytes, received {offset}.");
            }
            return offset;
        }

    }
}
