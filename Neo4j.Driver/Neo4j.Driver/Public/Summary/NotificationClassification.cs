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
/// This is a preview API, This API may change between minor revisions.<br/>
/// Represents the classification of server notifications surfaced by <see cref="IGqlStatusObject"/>.<br/> Used in
/// conjunction with <see cref="NotificationSeverity"/>.
/// </summary>
/// <since>5.23.0</since>
public enum NotificationClassification
{
    /// <summary>the <see cref="IGqlStatusObject"/>'s classification is a value unknown to this driver version.</summary>
    Unknown,

    /// <summary>The given hint cannot be satisfied.</summary>
    Hint,

    /// <summary>The query or command mentions entities that are unknown to the system.</summary>
    Unrecognized,

    /// <summary>
    /// The query/command is trying to use features that are not supported by the current system or using features
    /// that are experimental and should not be used in production.
    /// </summary>
    Unsupported,

    /// <summary>The query uses costly operations and might be slow.</summary>
    Performance,

    /// <summary>The query/command use deprecated features that should be replaced.</summary>
    Deprecation,

    /// <summary>The result of the query or command indicates a potential security issue.</summary>
    Security,

    /// <summary>
    /// Topology notifications provide additional information related to managing databases and servers.
    /// </summary>
    Topology,

    /// <summary>
    /// Schema notifications provide additional information related to managing indexes and constraints.
    /// </summary>
    Schema,

    /// <summary>Notification not covered by other categories.</summary>
    Generic
}
