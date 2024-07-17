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
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver;

/// <summary>
/// The GQL-status object as defined by the GQL standard.
/// </summary>
/// <since>5.22.0</since>
/// <seealso cref="Notification">Notification subtype of the GQL-status object</seealso>
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
    /// Returns the diagnostic record.
    /// </summary>
    /// <returns>The diagnostic record.</returns>
    IDictionary<string, object> DiagnosticRecord { get; }
}
