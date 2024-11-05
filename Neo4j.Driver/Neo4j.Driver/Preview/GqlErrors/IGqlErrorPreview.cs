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

using System.Collections.Generic;

namespace Neo4j.Driver.Preview.GqlErrors;

/// <summary>
/// Allows users to preview the GQL error functionality. This is a preview feature and may change in the future.
/// </summary>
public interface IGqlErrorPreview
{
    /// <summary>
    /// Gets or sets the GQL status of the exception.
    /// </summary>
    public string GqlStatus { get; }

    /// <summary>
    /// Gets or sets the GQL status description of the exception.
    /// </summary>
    public string GqlStatusDescription { get; }

    /// <summary>
    /// Gets or sets the GQL classification of the exception.
    /// </summary>
    public string GqlClassification { get; }

    /// <summary>
    /// The raw classification as received from the server.
    /// </summary>
    public string GqlRawClassification { get; }

    /// <summary>
    /// GqlDiagnosticRecord returns further information about the status for diagnostic purposes.
    /// GqlDiagnosticRecord is part of the GQL compliant errors preview feature.
    /// </summary>
    public Dictionary<string, object> GqlDiagnosticRecord { get; }
}

public static class Neo4jExceptionExtensions
{
    public static IGqlErrorPreview GetGqlErrorPreview(this Neo4jException exception)
    {
        return exception;
    }
}
