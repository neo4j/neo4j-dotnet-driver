// Copyright (c) 2002-2017 "Neo Technology,"
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
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Result
{
    internal class RecordSet : IRecordSet
    {
        private readonly Func<bool> _atEnd;
        internal int Position = 0;
        private readonly IList<IRecord> _records;

        public RecordSet(IList<IRecord> records, Func<bool> atEnd)
        {
            _records = records;
            _atEnd = atEnd;
        }

        public bool AtEnd => _atEnd();

        public IEnumerable<IRecord> Records()
        {
            while (!AtEnd || Position <= _records.Count)
            {
                while (Position == _records.Count)
                {
                    Task.Delay(50).Wait();
                    if (AtEnd && Position == _records.Count)
                        yield break;
                }

                yield return _records[Position++];
//                Position++;
            }
        }

        public IRecord Peek()
        {
            while (Position >= _records.Count) // Peeking record not received
            {
                if (AtEnd && Position >= _records.Count)
                {
                    return null;
                }

                Task.Delay(50).Wait();
            }

            return _records[Position];
        }
    }
}
