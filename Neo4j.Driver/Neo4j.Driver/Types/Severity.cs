﻿// Copyright (c) "Neo4j"
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

namespace Neo4j.Driver;

/// <summary>
/// Used In conjunction with <see cref="Category"/> to filter which <see cref="INotification"/>s will be sent in
/// <see cref="IResultSummary.Notifications"/>.<br/><br/> Can be used in <see cref="ConfigBuilder.WithNotifications"/> and
/// <see cref="SessionConfigBuilder.WithNotifications"/>.
/// </summary>
public enum Severity
{
    /// <summary>Request warning severity notifications. <br/> Neo4j recommends user intervention for all warning notifications</summary>
    /// <remarks>Will be returned as <see cref="NotificationSeverity.Warning"/></remarks>
    Warning,

    /// <summary>
    /// Request information severity notifications. <br/> Information notifications are for providing additional
    /// information.
    /// </summary>
    /// <remarks>Will be returned as <see cref="NotificationSeverity.Information"/></remarks>
    Information
}
