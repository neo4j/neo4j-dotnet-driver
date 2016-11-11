// Copyright (c) 2002-2016 "Neo Technology,"
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
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Result
{
    internal class RecordSet : IRecordSet
    {
        private readonly Func<IRecord> _nextRecordFunc;
        private IRecord _peekedRecord;

        public RecordSet(Func<IRecord> nextRecordFunc)
        {
            _nextRecordFunc = nextRecordFunc;
        }

        public bool AtEnd { get; private set; }

        public IEnumerable<IRecord> Records()
        {
            while (!HasReadAllRecords())
            {
                // first try to return if already retrived,
                // otherwise pull from input stream

                if (_peekedRecord != null)
                {
                    var record = _peekedRecord;
                    _peekedRecord = null;
                    yield return record;
                }
                else
                {
                    IRecord record = null;
                    try
                    {
                        record = _nextRecordFunc.Invoke();
                    }
                    finally
                    {
                        if (record == null)
                        {
                            AtEnd = true;
                        }
                    }
                    if (!AtEnd)
                    {
                        yield return record;
                    }
                }
            }
        }

        private bool HasReadAllRecords()
        {
            return AtEnd && _peekedRecord == null;
        }

        public IRecord Peek()
        {
            // we did not move the cursor in the stream
            if (_peekedRecord != null)
            {
                return _peekedRecord;
            }
            // we already arrived at the end of the stream
            if (AtEnd)
            {
                return null;
            }
            // we still in the middle of the stream and we need to pull from input buffer
            _peekedRecord = _nextRecordFunc.Invoke();
            if (_peekedRecord == null) // well the message received is a success
            {
                AtEnd = true;
                return null;
            }
            else // we get another record message
            {
                return _peekedRecord;
            }
        }
    }
}
