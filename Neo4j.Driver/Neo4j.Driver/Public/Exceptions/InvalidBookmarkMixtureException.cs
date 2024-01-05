﻿// Copyright (c) "Neo4j"
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

using Neo4j.Driver.Internal.ExceptionHandling;

namespace Neo4j.Driver;

/// <summary>
/// The provided bookmark is invalid. To recover from this a new session needs to be created.
/// </summary>
[ErrorCode("Neo.ClientError.Transaction.InvalidBookmarkMixture")]
public class InvalidBookmarkMixtureException : ClientException
{
    /// <summary>
    /// Create a new <see cref="InvalidBookmarkMixtureException" /> with an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidBookmarkMixtureException(string message) : base(message)
    {
    }
}
