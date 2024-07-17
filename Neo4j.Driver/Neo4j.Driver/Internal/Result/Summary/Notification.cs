using System;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Result;

internal class Notification : GqlStatusObject, INotification
{
    public Notification(
        string gqlStatus,
        string statusDescription,
        IDictionary<string, object> diagnosticRecord,
        string code,
        string title,
        string description,
        IInputPosition position,
        string rawSeverity,
        string rawCategory) : base (gqlStatus, statusDescription, diagnosticRecord)
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
        try
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
                var _ => NotificationCategory.Unknown
            };
        }
        catch
        {
            return NotificationCategory.Unknown;
        }
    }

    public static NotificationSeverity ParseSeverity(string severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "information" => NotificationSeverity.Information,
            "warning" => NotificationSeverity.Warning,
            var _ => NotificationSeverity.Unknown
        };
    }

    public override string ToString()
    {
        const string space = " ";
        const string equals = "=";

        return string.Concat(
            nameof(Notification),
            "{",
            nameof(GqlStatus), equals, GqlStatus, space,
            nameof(StatusDescription), equals, StatusDescription, space,
            nameof(DiagnosticRecord), equals, DiagnosticRecord.ToContentString(), space,
            nameof(Code), equals, Code, space,
            nameof(Title), equals, Title, space,
            nameof(Description), equals, Description, space,
            nameof(Position), equals, Position.ToString(), space,
            nameof(SeverityLevel), equals, SeverityLevel.ToString(), space,
            nameof(Category), equals, Category.ToString(), space,
            nameof(RawSeverityLevel), equals, RawSeverityLevel, space,
            nameof(RawCategory), equals, RawCategory, //no space
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
        int hash = 17;
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
