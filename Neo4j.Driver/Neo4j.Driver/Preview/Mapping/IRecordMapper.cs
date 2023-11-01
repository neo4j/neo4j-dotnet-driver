// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

namespace Neo4j.Driver.Preview.Mapping;

/// <summary>
/// Interface to be implemented by a class that maps records to objects of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of object to which records will be mapped.</typeparam>
public interface IRecordMapper<out T>
{
    /// <summary>
    /// Maps the given record to an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="record">The record to map.</param>
    /// <returns>The mapped object.</returns>
    T Map(IRecord record);
}
