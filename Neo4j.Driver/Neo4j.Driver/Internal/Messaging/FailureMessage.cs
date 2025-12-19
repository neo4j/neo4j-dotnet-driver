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
using System.Collections.Generic;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.MessageHandling;

namespace Neo4j.Driver.Internal.Messaging;

internal sealed class FailureMessage : IResponseMessage
{
    public FailureMessage()
    {
    }

    public FailureMessage(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>Code is the Neo4j-specific error code, now deprecated in favor of <see cref="GqlStatus"/>.</summary>
    [Obsolete("This property is deprecated and will be removed in future versions. Use GqlStatus instead.")]
    public string Code { get; set; }

    /// <summary>The specific error message describing the failure.</summary>
    public string Message { get; set; }

    /// <summary>Returns the GQLSTATUS.</summary>
    public string GqlStatus { get; set; }

    /// <summary>Provides a standard description for the associated GQLStatus code.</summary>
    public string GqlStatusDescription { get; set; }

    /// <summary>A high-level categorization of the error, specific to GQL error handling.</summary>
    public string GqlClassification { get; set; }

    /// <summary>The raw classification as received from the server.</summary>
    public string GqlRawClassification { get; set; }

    /// <summary>
    /// GqlDiagnosticRecord returns further information about the status for diagnostic purposes. GqlDiagnosticRecord
    /// is part of the GQL compliant errors preview feature.
    /// </summary>
    public Dictionary<string, object> GqlDiagnosticRecord { get; set; }

    /// <summary>
    /// GqlCause represents the underlying error, if any, which caused the current error. GqlCause is part of the GQL
    /// compliant errors preview feature (see README on what it means in terms of support and compatibility guarantees)
    /// </summary>
    public FailureMessage GqlCause { get; set; }

    public void Dispatch(IResponsePipeline pipeline)
    {
        pipeline.OnFailure(this);
    }

    public IPackStreamSerializer Serializer => FailureMessageSerializer.Instance;

    public override string ToString()
    {
        return $"FAILURE gqlStatus={GqlStatus}, message={Message}";
    }
}
