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

#nullable enable

using System;
using Neo4j.Driver.Internal.GqlCompliance;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Preview.Encryption;

/// <summary>
/// Represents a property encryption failure: an error encountered while resolving encryption keys,
/// performing encryption or decryption, or otherwise processing encrypted property values.
/// </summary>
public class PropertyEncryptionException : ClientException
{
    /// <summary>Create a new <see cref="PropertyEncryptionException"/> with an error message.</summary>
    /// <param name="message">The error message.</param>
    public PropertyEncryptionException(string message) : this(message, null)
    {
    }

    /// <summary>Create a new <see cref="PropertyEncryptionException"/> with an error message and a cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The original error that caused this failure.</param>
    public PropertyEncryptionException(string message, Exception? innerException)
        : base(NewFailureMessage(message), innerException)
    {
    }

    private static FailureMessage NewFailureMessage(string message)
    {
        return new FailureMessage
        {
            Message = message,
            GqlStatus = GqlErrors.UnknownGqlStatus,
            GqlStatusDescription = $"{GqlErrors.UnknownGqlStatusDescription} {message}",
            GqlClassification = GqlErrors.UnknownError,
            GqlDiagnosticRecord = GqlErrors.NewDefaultDiagnosticRecord()
        };
    }
}
