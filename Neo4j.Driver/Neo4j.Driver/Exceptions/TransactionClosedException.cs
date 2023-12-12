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
/// The exception that is thrown when calling <see cref="IAsyncTransaction.CommitAsync"/> or <see cref="IAsyncTransaction.RollbackAsync"/>
/// on an <see cref="IAsyncTransaction"/> that has already been closed.
/// </summary>
[DataContract]
public class TransactionClosedException : ClientException
{
    /// <summary>
    ///  Create a new <see cref="TransactionClosedException"/> with an error message.
    /// </summary>
    /// <param name="message">The error message</param>
    public TransactionClosedException(string message) : base(message)
    {
    }
}
