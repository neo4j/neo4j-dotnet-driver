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

namespace Neo4j.Driver.Types;

/// <summary>
///     Used to specify what notifications to receive from the server.
/// </summary>
public enum NotificationFilter
{
    /// <summary>
    ///     Receive no notifications.
    /// </summary>
    None,

    /// <summary>
    ///     Receive all notifications.
    /// </summary>
    All,

    /// <summary>
    ///     Receive all query notifications.
    /// </summary>
    AllQuery,

    /// <summary>
    ///     Receive all warning notifications.
    /// </summary>
    WarningAll,

    /// <summary>
    ///     Receive only deprecation notifications.
    /// </summary>
    WarningDeprecation,

    /// <summary>
    ///     Receive only warning hint notifications.
    /// </summary>
    WarningHint,

    /// <summary>
    ///     Receive only query notifications at a warning level.
    /// </summary>
    WarningQuery,

    /// <summary>
    ///     Receive only unsupported warning notifications.
    /// </summary>
    WarningUnsupported,

    /// <summary>
    ///     Receive all information notifications.
    /// </summary>
    InformationAll,

    /// <summary>
    ///     Receive only runtime information notifications.
    /// </summary>
    InformationRuntime,

    /// <summary>
    ///     Receive only query notifications at a information level.
    /// </summary>
    InformationQuery,

    /// <summary>
    ///     Receive only performance notifications.
    /// </summary>
    InformationPerformance
}