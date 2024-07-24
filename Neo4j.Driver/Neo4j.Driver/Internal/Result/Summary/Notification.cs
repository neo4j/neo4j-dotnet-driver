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

namespace Neo4j.Driver.Internal.Result;

internal sealed class Notification : INotification
{
    public Notification(
        string code,
        string title,
        string description,
        IInputPosition position,
        string rawSeverity,
        string rawCategory)
    {
        Code = code;
        Title = title;
        Description = description;
        Position = position;
        RawSeverityLevel = rawSeverity;
        RawCategory = rawCategory;
    }

    public string RawSeverityLevel { get; }
    public NotificationSeverity SeverityLevel => ParseSeverity(RawSeverityLevel);
    public string RawCategory { get; }
    public NotificationCategory Category => ParseCategory(RawCategory);
    public string Code { get; }
    public string Title { get; }
    public string Description { get; }
    public IInputPosition Position { get; }

    [Obsolete("Deprecated, Replaced by RawSeverityLevel. Will be removed in 6.0")]
    public string Severity => RawSeverityLevel;

    private NotificationCategory ParseCategory(string category)
    {
        return category?.ToLowerInvariant() switch
        {
            "hint" => NotificationCategory.Hint,
            "unrecognized" => NotificationCategory.Unrecognized,
            "unsupported" => NotificationCategory.Unsupported,
            "performance" => NotificationCategory.Performance,
            "deprecation" => NotificationCategory.Deprecation,
            "security" => NotificationCategory.Security,
            "topology" => NotificationCategory.Topology,
            "generic" => NotificationCategory.Generic,
            _ => NotificationCategory.Unknown
        };
    }

    public static NotificationSeverity ParseSeverity(string severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "information" => NotificationSeverity.Information,
            "warning" => NotificationSeverity.Warning,
            _ => NotificationSeverity.Unknown
        };
    }

    public override string ToString()
    {
        const string space = " ";
        const string equals = "=";

        return string.Concat(
            nameof(Notification),
            "{",
            nameof(Code),
            equals,
            Code,
            space,
            nameof(Title),
            equals,
            Title,
            space,
            nameof(Description),
            equals,
            Description,
            space,
            nameof(Position),
            equals,
            Position?.ToString(),
            space,
            nameof(SeverityLevel),
            equals,
            SeverityLevel.ToString(),
            space,
            nameof(Category),
            equals,
            Category.ToString(),
            space,
            nameof(RawSeverityLevel),
            equals,
            RawSeverityLevel,
            space,
            nameof(RawCategory),
            equals,
            RawCategory, //no space
            "}");
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj == null || GetType() != obj.GetType() || !base.Equals(obj))
        {
            return false;
        }

        var other = (Notification)obj;
        return Code == other.Code &&
            Title == other.Title &&
            Description == other.Description &&
            SeverityLevel == other.SeverityLevel &&
            RawSeverityLevel == other.RawSeverityLevel &&
            Category == other.Category &&
            RawCategory == other.RawCategory &&
            Equals(Position, other.Position);
    }

    public override int GetHashCode()
    {
        var hash = 17;
        hash = hash * 23 + (Code?.GetHashCode() ?? 0);
        hash = hash * 23 + (Title?.GetHashCode() ?? 0);
        hash = hash * 23 + (Description?.GetHashCode() ?? 0);
        hash = hash * 23 + SeverityLevel.GetHashCode();
        hash = hash * 23 + (RawSeverityLevel?.GetHashCode() ?? 0);
        hash = hash * 23 + Category.GetHashCode();
        hash = hash * 23 + (RawCategory?.GetHashCode() ?? 0);
        hash = hash * 23 + (Position?.GetHashCode() ?? 0);
        return hash;
    }
}
