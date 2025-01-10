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
/// <see cref="Severity"/> to filter which <see cref="IGqlStatusObject"/> and <see cref="INotification"/>s will be sent in
/// <see cref="IResultSummary.GqlStatusObjects"/> and  <see cref="IResultSummary.Notifications"/> .<br/> Can be used in
/// <see cref="ConfigBuilder.WithNotifications(Severity?, Category[], Classification[])"/> and
/// <see cref="SessionConfigBuilder.WithNotifications(Severity?, Category[], Classification[])"/>.
/// </summary>
/// <since>5.23.0</since>
public enum Classification
{
    /// <summary>Receive notifications when a hint in query cannot be satisfied.</summary>
    /// <remarks>
    /// Returned as <see cref="NotificationClassification.Hint"/> in <see cref="IGqlStatusObject.Classification"/> and
    /// as <see cref="NotificationCategory.Hint"/> in <see cref="INotification.Category"/>.
    /// </remarks>
    Hint,

    /// <summary>Receive notifications when a query or command mentions entities that are unknown to the system.</summary>
    /// <remarks>
    /// Returned as <see cref="NotificationClassification.Unrecognized"/> in
    /// <see cref="IGqlStatusObject.Classification"/> and as <see cref="NotificationCategory.Unrecognized"/> in
    /// <see cref="INotification.Category"/>.
    /// </remarks>
    Unrecognized,

    /// <summary>
    /// Receive notifications when a query/command is trying to use features that are not supported by the current
    /// system or using features that are experimental and should not be used in production.
    /// </summary>
    /// <remarks>
    /// Returned as <see cref="NotificationClassification.Unsupported"/> in
    /// <see cref="IGqlStatusObject.Classification"/> and as <see cref="NotificationCategory.Unsupported"/> in
    /// <see cref="INotification.Category"/>.
    /// </remarks>
    Unsupported,

    /// <summary>Receive notifications when a query uses costly operations and might be slow.</summary>
    /// <remarks>
    /// Returned as <see cref="NotificationClassification.Performance"/> in
    /// <see cref="IGqlStatusObject.Classification"/> and as <see cref="NotificationCategory.Performance"/> in
    /// <see cref="INotification.Category"/>.
    /// </remarks>
    Performance,

    /// <summary>Receive notifications when a query/command use deprecated features that should be replaced.</summary>
    /// <remarks>
    /// Returned as <see cref="NotificationClassification.Deprecation"/> in
    /// <see cref="IGqlStatusObject.Classification"/> and as <see cref="NotificationCategory.Deprecation"/> in
    /// <see cref="INotification.Category"/>.
    /// </remarks>
    Deprecation,

    /// <summary>Receive notifications when the result of the query or command indicates a potential security issue.</summary>
    /// <remarks>
    /// Returned as <see cref="NotificationClassification.Security"/> in <see cref="IGqlStatusObject.Classification"/>
    /// and as <see cref="NotificationCategory.Security"/> in <see cref="INotification.Category"/>.
    /// </remarks>
    Security,

    /// <summary>Receive notifications related to managing databases and servers.</summary>
    /// <remarks>
    /// Returned as <see cref="NotificationClassification.Topology"/> in <see cref="IGqlStatusObject.Classification"/>
    /// and as <see cref="NotificationCategory.Topology"/> in <see cref="INotification.Category"/>.
    /// </remarks>
    Topology,

    /// <summary>Receive notifications related to managing indexes and constraints.</summary>
    /// <remarks> Returned as <see cref="NotificationClassification.Schema"/> in <see cref="IGqlStatusObject.Classification"/>
    /// and as <see cref="NotificationCategory.Schema"/> in <see cref="INotification.Category"/>.
    /// </remarks>
    Schema,

    /// <summary>Receive notifications not covered by other categories.</summary>
    /// <remarks>
    /// Returned as <see cref="NotificationClassification.Generic"/> in <see cref="IGqlStatusObject.Classification"/>
    /// and as <see cref="NotificationCategory.Generic"/> in <see cref="INotification.Category"/>.
    /// </remarks>
    Generic
}
