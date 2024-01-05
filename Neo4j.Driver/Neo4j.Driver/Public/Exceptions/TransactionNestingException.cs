// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
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

using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// An attempt to BeginTransaction has been made before the sessions existing transaction
/// has been consumed or rolled back. e.g. An attempt to nest transactions has occurred.
/// A session can only have a single transaction at a time.
/// </summary>
[DataContract]
public class TransactionNestingException : ClientException
{ 
    /// <summary>
    /// Create a new <see cref="TransactionNestingException"/> with an error message
    /// </summary>
    /// <param name="message">The error message</param>
    public TransactionNestingException(string message) : base(message)		
    {
    }
}
