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
/// Used In conjunction with <see cref="Severity"/> to filter
/// which <see cref="INotification"/>s will be sent in <see cref="IResultSummary.Notifications"/>.<br/><br/>
/// Can be used in <see cref="ConfigBuilder.WithNotificationFilters"/>
/// and <see cref="SessionConfigBuilder.WithNotificationFilters"/>.
/// </summary>
public enum Category
{
    /// <summary>
    /// Receive notifications when a hint in query cannot be satisfied.
    /// </summary>
    /// <remarks>Returned as <see cref="NotificationCategory.Hint"/></remarks>
    Hint,
    /// <summary>
    /// Receive notifications when a query or command mentions entities that are unknown to the system.
    /// </summary>
    /// <remarks>Returned as <see cref="NotificationCategory.Unrecognized"/></remarks>
    Unrecognized,
    /// <summary>
    /// Receive notifications when a query/command is trying to use features that are not supported by the current system or using features that are experimental and should not be used in production.
    /// </summary>
    /// <remarks>Returned as <see cref="NotificationCategory.Unsupported"/></remarks>
    Unsupported,
    /// <summary>
    /// Receive notifications when a query uses costly operations and might be slow.
    /// </summary>
    /// <remarks>Returned as <see cref="NotificationCategory.Performance"/></remarks>
    Performance,
    /// <summary>
    /// Receive notifications when a query/command use deprecated features that should be replaced
    /// </summary>
    /// <remarks>Returned as <see cref="NotificationCategory.Deprecation"/></remarks>
    Deprecation,
    /// <summary>
    /// Receive notifications not covered by other categories.
    /// </summary>
    /// <remarks>Returned as <see cref="NotificationCategory.Generic"/></remarks>
    Generic
}
