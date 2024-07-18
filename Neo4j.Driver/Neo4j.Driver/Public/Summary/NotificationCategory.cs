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

namespace Neo4j.Driver;

/// <summary>
/// Represents the category of server notifications surfaced by <see cref="IGqlStatusObject"/>.<br/> Used in
/// conjunction with <see cref="NotificationSeverity"/>.
/// </summary>
public enum NotificationClassification
{
    /// <summary>the <see cref="INotification"/>'s category is a value unknown to this driver version.</summary>
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

    /// <summary>Notification not covered by other categories.</summary>
    Generic
}

public struct NotificationCategory
{
    /// <summary>
    /// Determines whether the specified <see cref="NotificationCategory"/> is equal to the current
    /// <see cref="NotificationCategory"/>.
    /// </summary>
    public bool Equals(NotificationCategory other)
    {
        return _notificationClassification == other._notificationClassification;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is NotificationCategory other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return (int)_notificationClassification;
    }

    public static NotificationCategory Unknown => new(NotificationClassification.Unknown);
    public static NotificationCategory Hint => new(NotificationClassification.Hint);
    public static NotificationCategory Unrecognized => new(NotificationClassification.Unrecognized);
    public static NotificationCategory Unsupported => new(NotificationClassification.Unsupported);
    public static NotificationCategory Performance => new(NotificationClassification.Performance);
    public static NotificationCategory Deprecation => new(NotificationClassification.Deprecation);
    public static NotificationCategory Security => new(NotificationClassification.Security);
    public static NotificationCategory Topology => new(NotificationClassification.Topology);
    public static NotificationCategory Generic => new(NotificationClassification.Generic);

    private NotificationClassification _notificationClassification;

    public NotificationCategory(NotificationClassification notificationClassification)
    {
        _notificationClassification = notificationClassification;
    }

    // implicit casts to and from NotificationClassification
    public static implicit operator NotificationClassification(NotificationCategory category) => category._notificationClassification;
    public static implicit operator NotificationCategory(NotificationClassification notificationClassification) => new(notificationClassification);

    /// <inheritdoc />
    public override string ToString() => _notificationClassification.ToString();

    public static bool operator ==(NotificationCategory a, NotificationCategory b)
    {
        return a._notificationClassification == b._notificationClassification;
    }

    // and the != operator
    public static bool operator !=(NotificationCategory a, NotificationCategory b)
    {
        return a._notificationClassification != b._notificationClassification;
    }
}
