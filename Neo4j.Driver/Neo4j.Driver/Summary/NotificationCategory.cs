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
    /// 
    /// </summary>
    Hint,
    /// <summary>
    /// 
    /// </summary>
    Unrecognized,
    /// <summary>
    /// 
    /// </summary>
    Unsupported,
    /// <summary>
    /// 
    /// </summary>
    Performance,
    /// <summary>
    /// 
    /// </summary>
    Deprecation,
    /// <summary>
    /// 
    /// </summary>
    Generic,
    /// <summary>
    /// <see cref="INotification"/>'s category is a value unknown to this driver version.
    /// </summary>
    Unknown
}
