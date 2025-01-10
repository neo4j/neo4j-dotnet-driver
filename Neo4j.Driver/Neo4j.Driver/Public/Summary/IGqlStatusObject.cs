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

namespace Neo4j.Driver;

/// <summary>
/// This is a preview API, This API may change between minor revisions.<br/>
/// The GQL-status object as defined by the GQL standard. Returned by <see cref="IResultSummary.GqlStatusObjects"/>
/// </summary>
/// <seealso cref="IResultSummary.GqlStatusObjects" />
/// <since>5.23.0</since>
public interface IGqlStatusObject
{
    /// <summary>
    /// Returns the GQLSTATUS as defined by the GQL standard.
    /// </summary>
    /// <returns>The GQLSTATUS value.</returns>
    string GqlStatus { get; }

    /// <summary>
    /// The GQLSTATUS description.
    /// </summary>
    /// <returns>The GQLSTATUS description.</returns>
    string StatusDescription { get; }
    
    /// <summary>
    /// Gets the position in the query where the <see cref="IGqlStatusObject"/> instance points to. Not all notifications
    /// have a unique position to point to and in that case the position would be set to all 0s.
    /// </summary>
    IInputPosition Position { get; }

    /// <summary>Gets the parsed <see cref="RawClassification"/> of the <see cref="IGqlStatusObject"/> instance.</summary>
    NotificationClassification Classification { get; }

    /// <summary>Gets the unparsed string value for <see cref="Classification"/> of the <see cref="IGqlStatusObject"/> instance.</summary>
    string RawClassification { get; }

    /// <summary>Gets the parsed <see cref="RawSeverity"/> of the <see cref="IGqlStatusObject"/> instance.</summary>
    NotificationSeverity Severity { get; }

    /// <summary>Gets the unparsed string value for <see cref="Severity"/> of the <see cref="IGqlStatusObject"/> instance.</summary>
    string RawSeverity { get; }

    /// <summary>
    /// Gets the GQL Status diagnostic record.
    /// </summary>
    /// <returns>The diagnostic record as a dictionary.</returns>
    IReadOnlyDictionary<string, object> DiagnosticRecord { get; }

    /// <summary>
    /// Gets the GQL Status as string representation
    /// </summary>
    string RawDiagnosticRecord { get; }
}
