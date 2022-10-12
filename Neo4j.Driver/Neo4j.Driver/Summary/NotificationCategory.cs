// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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

namespace Neo4j.Driver;

/// <summary>
/// Represents the category of server notifications surfaced by <see cref="INotification"/>.<br/>
/// Used in conjunction with <see cref="NotificationSeverity"/>.
/// </summary>
public enum NotificationCategory
{
    /// <summary>
    /// A hint specified in the query cannot be satisfied.
    /// </summary>
    Hint,
    /// <summary>
    /// An possible issue is identified in the query.<br/>
    /// <see cref="INotification"/> will contain more details.
    /// </summary>
    Query,
    /// <summary>
    /// Using an unsupported feature.<br/>
    /// Unsupported features are not recommended for usage in production code.
    /// </summary>
    Unsupported,
    /// <summary>
    /// Performance of query is sub-optimal.
    /// </summary>
    Performance,
    /// <summary>
    /// Use of a deprecated feature, format or functionality identified in the query.
    /// </summary>
    Deprecation,
    /// <summary>
    /// The outcome of the operation is impacted by the system's current status.
    /// </summary>
    Runtime,
    /// <summary>
    /// <see cref="INotification"/>'s category is a value unknown to this driver version.
    /// </summary>
    Unknown
}
