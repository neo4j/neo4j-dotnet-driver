// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

using System;
using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// The exception that is thrown when trying to further interact with a terminated transaction.
/// Transactions are terminated when they incur errors. <br/>
/// If created by the driver the <see cref="Neo4jException.Code"/> will be null.
/// </summary>
[DataContract]
public sealed class TransactionTerminatedException : ClientException
{
    /// <inheritdoc />
    public override bool IsRetriable => (InnerException as Neo4jException)?.IsRetriable ?? false;

    internal TransactionTerminatedException(Exception inner) :
        base((inner as Neo4jException)?.Code, inner.Message, inner)
    {
    }
}
