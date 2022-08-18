// Copyright (c) 2002-2022 "Neo4j,"
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
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

internal class RxCursorWrapper : IResultCursor
{
    private readonly IRxResult _cursor;
    private IList<IRecord> values;
    private int position;
    private Exception caught;
    private bool read;
    private IEnumerator<IRecord> records;

    public RxCursorWrapper(IRxResult cursor)
    {
        _cursor = cursor;
        position = -1;
        read = false;

    }

    public Task<string[]> KeysAsync()
    {
        return _cursor.Keys().ToTask();
    }

    public Task<IResultSummary> ConsumeAsync()
    {
        return _cursor.Consume().ToTask();
    }

    public Task<IRecord> PeekAsync()
    {
        //Read();
        //records.
        //return Task.FromResult(position + 1 == values.Count ? null : values[position + 1]);
        throw new NotImplementedException();
    }

    public Task<bool> FetchAsync()
    {
        Read();
        return Task.FromResult(records.MoveNext());
    }

    private void Read()
    {
        if (!read)
        {
            read = true;
            try
            {
                records = _cursor.Records().Next().GetEnumerator();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        }

        if (caught != null)
            throw caught;
    }

    public IRecord Current => records.Current;

    public bool IsOpen => _cursor.IsOpen.Wait();
}