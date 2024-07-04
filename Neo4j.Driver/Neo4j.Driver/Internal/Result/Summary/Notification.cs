using System;

namespace Neo4j.Driver.Internal.Result;

internal class Notification : INotification
{
    public Notification(
        string code,
        string title,
        string description,
        IInputPosition position,
        string severity,
        string rawCategory)
    {
        Code = code;
        Title = title;
        Description = description;
        Position = position;
        RawSeverityLevel = severity;
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

    private NotificationSeverity ParseSeverity(string severity)
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
            Position.ToString(),
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
}