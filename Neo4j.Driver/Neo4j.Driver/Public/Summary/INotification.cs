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

namespace Neo4j.Driver;

/// <summary>
/// Representation for notifications found when executing a query. A notification can be visualized in a client
/// pinpointing problems or other information about the query.
/// </summary>
public interface INotification : IGqlStatusObject
{
    /// <summary>Gets the notification code of the <see cref="INotification"/> instance.</summary>
    string Code { get; }

    /// <summary>Gets the condensed summary of the <see cref="INotification"/> instance.</summary>
    string Title { get; }

    /// <summary>Gets the full description of the <see cref="INotification"/> instance.</summary>
    string Description { get; }

    /// <summary>
    /// Gets the position in the query where the <see cref="INotification"/> instance points to. Not all notifications
    /// have a unique position to point to and in that case the position would be set to all 0s.
    /// </summary>
    IInputPosition Position { get; }

    /// <summary>Gets the severity level of the <see cref="INotification"/> instance.</summary>
    [Obsolete("Deprecated, Replaced by RawSeverityLevel. Will be removed in 6.0")]
    string Severity { get; }

    /// <summary>Gets the unparsed string value for <see cref="SeverityLevel"/> of the <see cref="INotification"/> instance.</summary>
    public string RawSeverityLevel { get; }

    /// <summary>Gets the unparsed string value for <see cref="Category"/> of the <see cref="INotification"/> instance.</summary>
    string RawCategory { get; }

    /// <summary>Gets the parses <see cref="RawSeverityLevel"/> of the <see cref="INotification"/> instance.</summary>
    NotificationSeverity SeverityLevel { get; }

    /// <summary>Gets the parsed <see cref="RawCategory"/> of the <see cref="INotification"/> instance.</summary>
    NotificationCategory Category { get; }
}
