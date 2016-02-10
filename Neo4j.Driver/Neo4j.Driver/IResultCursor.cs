//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System.Collections.Generic;

namespace Neo4j.Driver
{
    public interface IResultCursor
    {
        IEnumerable<Record> Stream();
        IResultSummary Summary { get; }
        Record Record();
        long Position { get; }
        bool Next();
    }

    public interface IExtendedResultCursor : IResultCursor
    {
        bool AtEnd { get; }
        long Skip(long records);
        long Limit(long records);
        bool First();
        bool Single();
        Record Peek();
    }
}