// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal.Result;

internal class SummaryBuilder
{
    public SummaryBuilder(Query query, IServerInfo serverInfo)
    {
        Query = query ?? throw new ArgumentNullException(nameof(query));
        Server = serverInfo;
    }

    public Query Query { get; }
    public IServerInfo Server { get; }

    public virtual QueryType QueryType { get; set; }
    public virtual ICounters Counters { get; set; }
    public virtual IPlan Plan { get; set; }
    public virtual IProfiledPlan Profile { get; set; }
    public virtual IList<INotification> Notifications { get; set; }
    public virtual long ResultAvailableAfter { get; set; } = -1L;
    public virtual long ResultConsumedAfter { get; set; } = -1L;
    public virtual IDatabaseInfo Database { get; set; }

    public IResultSummary Build()
    {
        return new ResultSummary(this);
    }

    private class ResultSummary : IResultSummary
    {
        public ResultSummary(SummaryBuilder builder)
        {
            Query = builder.Query;
            QueryType = builder.QueryType;
            Counters = builder.Counters ?? new Counters();
            Profile = builder.Profile;
            Plan = Profile ?? builder.Plan;
            Notifications = builder.Notifications;
            ResultAvailableAfter = TimeSpan.FromMilliseconds(builder.ResultAvailableAfter);
            ResultConsumedAfter = TimeSpan.FromMilliseconds(builder.ResultConsumedAfter);
            Server = builder.Server;
            Database = builder.Database ?? new DatabaseInfo();
        }

        public Query Query { get; }
        public ICounters Counters { get; }
        public QueryType QueryType { get; }
        public bool HasPlan => Plan != null;
        public bool HasProfile => Profile != null;
        public IPlan Plan { get; }
        public IProfiledPlan Profile { get; }
        public IList<INotification> Notifications { get; }
        public TimeSpan ResultAvailableAfter { get; }
        public TimeSpan ResultConsumedAfter { get; }
        public IServerInfo Server { get; }
        public IDatabaseInfo Database { get; }

        public override string ToString()
        {
            return $"{GetType().Name}{{{nameof(Query)}={Query}, " +
                $"{nameof(Counters)}={Counters}, " +
                $"{nameof(QueryType)}={QueryType}, " +
                $"{nameof(Plan)}={Plan}, " +
                $"{nameof(Profile)}={Profile}, " +
                $"{nameof(Notifications)}={Notifications.ToContentString()}, " +
                $"{nameof(ResultAvailableAfter)}={ResultAvailableAfter:g}, " +
                $"{nameof(ResultConsumedAfter)}={ResultConsumedAfter:g}, " +
                $"{nameof(Server)}={Server})," +
                $"{nameof(Database)}={Database}}}";
        }
    }
}

internal class ServerInfo : IServerInfo, IUpdateableInfo
{
    public ServerInfo(Uri uri)
    {
        Address = $"{uri.Host}:{uri.Port}";
    }

    internal BoltProtocolVersion Protocol { get; set; }

    public string ProtocolVersion => Protocol?.ToString() ?? "0.0";

    public string Agent { get; set; }

    public string Address { get; }

    public void Update(BoltProtocolVersion boltVersion, string agent)
    {
        Protocol = boltVersion;
        Agent = agent;
    }

    public override string ToString()
    {
        return $"{GetType().Name}{{{nameof(Address)}={Address}, " +
            $"{nameof(Agent)}={Agent}, " +
            $"{nameof(ProtocolVersion)}={ProtocolVersion}}}";
    }
}

internal interface IUpdateableInfo
{
    void Update(BoltProtocolVersion boltVersion, string agent);
}

internal class Plan : IPlan
{
    public Plan(
        string operationType,
        IDictionary<string, object> args,
        IList<string> identifiers,
        IList<IPlan> childPlans)
    {
        OperatorType = operationType;
        Arguments = args;
        Identifiers = identifiers;
        Children = childPlans;
    }

    public string OperatorType { get; }
    public IDictionary<string, object> Arguments { get; }
    public IList<string> Identifiers { get; }
    public IList<IPlan> Children { get; }

    public override string ToString()
    {
        return $"{GetType().Name}{{{nameof(OperatorType)}={OperatorType}, " +
            $"{nameof(Arguments)}={Arguments.ToContentString()}, " +
            $"{nameof(Identifiers)}={Identifiers.ToContentString()}, " +
            $"{nameof(Children)}={Children.ToContentString()}}}";
    }
}

internal class ProfiledPlan : IProfiledPlan
{
    public ProfiledPlan(
        string operatorType,
        IDictionary<string, object> arguments,
        IList<string> identifiers,
        IList<IProfiledPlan> children,
        long dbHits,
        long records,
        long pageCacheHits,
        long pageCacheMisses,
        double pageCacheHitRatio,
        long time,
        bool foundStats)
    {
        OperatorType = operatorType;
        Arguments = arguments;
        Identifiers = identifiers;
        Children = children;
        DbHits = dbHits;
        Records = records;
        PageCacheHits = pageCacheHits;
        PageCacheMisses = pageCacheMisses;
        PageCacheHitRatio = pageCacheHitRatio;
        HasPageCacheStats = foundStats;
        Time = time;
    }

    public string OperatorType { get; }

    public IDictionary<string, object> Arguments { get; }

    public IList<string> Identifiers { get; }

    public bool HasPageCacheStats { get; }
    IList<IPlan> IPlan.Children => Children.Cast<IPlan>().ToList();

    public IList<IProfiledPlan> Children { get; }

    public long DbHits { get; }

    public long Records { get; }
    public long PageCacheHits { get; }
    public long PageCacheMisses { get; }
    public double PageCacheHitRatio { get; }
    public long Time { get; }

    public override string ToString()
    {
        return $"{GetType().Name}{{{nameof(OperatorType)}={OperatorType}, " +
            $"{nameof(Arguments)}={Arguments.ToContentString()}, " +
            $"{nameof(Identifiers)}={Identifiers.ToContentString()}, " +
            $"{nameof(DbHits)}={DbHits}, " +
            $"{nameof(Records)}={Records}, " +
            $"{nameof(PageCacheHits)}={PageCacheHits}, " +
            $"{nameof(PageCacheMisses)}={PageCacheMisses}, " +
            $"{nameof(PageCacheHitRatio)}={PageCacheHitRatio}, " +
            $"{nameof(Time)}={Time}, " +
            $"{nameof(Children)}={Children.ToContentString()}}}";
    }
}

internal class Counters : ICounters
{
    private readonly bool? _containsUpdates;

    public Counters() : this(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, null)
    {
    }

    public Counters(
        int nodesCreated,
        int nodesDeleted,
        int relationshipsCreated,
        int relationshipsDeleted,
        int propertiesSet,
        int labelsAdded,
        int labelsRemoved,
        int indexesAdded,
        int indexesRemoved,
        int constraintsAdded,
        int constraintsRemoved,
        int systemUpdates,
        bool? containsSystemUpdates,
        bool? containsUpdates)
    {
        NodesCreated = nodesCreated;
        NodesDeleted = nodesDeleted;
        RelationshipsCreated = relationshipsCreated;
        RelationshipsDeleted = relationshipsDeleted;
        PropertiesSet = propertiesSet;
        LabelsAdded = labelsAdded;
        LabelsRemoved = labelsRemoved;
        IndexesAdded = indexesAdded;
        IndexesRemoved = indexesRemoved;
        ConstraintsAdded = constraintsAdded;
        ConstraintsRemoved = constraintsRemoved;
        SystemUpdates = systemUpdates;
        ContainsSystemUpdates = containsSystemUpdates ?? IsPositive(systemUpdates);
        _containsUpdates = containsUpdates;
    }

    public bool ContainsUpdates => _containsUpdates ??
    (
        IsPositive(NodesCreated) ||
        IsPositive(NodesDeleted) ||
        IsPositive(RelationshipsCreated) ||
        IsPositive(RelationshipsDeleted) ||
        IsPositive(PropertiesSet) ||
        IsPositive(LabelsAdded) ||
        IsPositive(LabelsRemoved) ||
        IsPositive(IndexesAdded) ||
        IsPositive(IndexesRemoved) ||
        IsPositive(ConstraintsAdded) ||
        IsPositive(ConstraintsRemoved));

    public int NodesCreated { get; }
    public int NodesDeleted { get; }
    public int RelationshipsCreated { get; }
    public int RelationshipsDeleted { get; }
    public int PropertiesSet { get; }
    public int LabelsAdded { get; }
    public int LabelsRemoved { get; }
    public int IndexesAdded { get; }
    public int IndexesRemoved { get; }
    public int ConstraintsAdded { get; }
    public int ConstraintsRemoved { get; }
    public int SystemUpdates { get; }

    public bool ContainsSystemUpdates { get; }

    private bool IsPositive(int value)
    {
        return value > 0;
    }

    public override string ToString()
    {
        return $"{GetType().Name}{{{nameof(NodesCreated)}={NodesCreated}, " +
            $"{nameof(NodesDeleted)}={NodesDeleted}, " +
            $"{nameof(RelationshipsCreated)}={RelationshipsCreated}, " +
            $"{nameof(RelationshipsDeleted)}={RelationshipsDeleted}, " +
            $"{nameof(PropertiesSet)}={PropertiesSet}, " +
            $"{nameof(LabelsAdded)}={LabelsAdded}, " +
            $"{nameof(LabelsRemoved)}={LabelsRemoved}, " +
            $"{nameof(IndexesAdded)}={IndexesAdded}, " +
            $"{nameof(IndexesRemoved)}={IndexesRemoved}, " +
            $"{nameof(ConstraintsAdded)}={ConstraintsAdded}, " +
            $"{nameof(ConstraintsRemoved)}={ConstraintsRemoved}, " +
            $"{nameof(SystemUpdates)}={SystemUpdates}}}";
    }
}

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
        Severity = severity;
        RawSeverityLevel = severity;
        RawCategory = rawCategory;
    }

    public string RawSeverityLevel { get; }
    public NotificationSeverity SeverityLevel => ParseSeverity(Severity);
    public string RawCategory { get; }
    public NotificationCategory Category => ParseCategory(RawCategory);
    public string Code { get; }
    public string Title { get; }
    public string Description { get; }
    public IInputPosition Position { get; }

    [Obsolete("Deprecated, Replaced by RawSeverityLevel. Will be removed in 6.0")]
    public string Severity { get; }

    private NotificationCategory ParseCategory(string category)
    {
        try
        {
            return category?.ToLower() switch
            {
                "hint" => NotificationCategory.Hint,
                "unrecognized" => NotificationCategory.Unrecognized,
                "unsupported" => NotificationCategory.Unsupported,
                "performance" => NotificationCategory.Performance,
                "deprecation" => NotificationCategory.Deprecation,
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
        return severity?.ToLower() switch
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

internal class InputPosition : IInputPosition
{
    public InputPosition(int offset, int line, int column)
    {
        Offset = offset;
        Line = line;
        Column = column;
    }

    public int Offset { get; }
    public int Line { get; }
    public int Column { get; }

    public override string ToString()
    {
        return $"{GetType().Name}{{{nameof(Offset)}={Offset}, " +
            $"{nameof(Line)}={Line}, " +
            $"{nameof(Column)}={Column}}}";
    }
}

internal class DatabaseInfo : IDatabaseInfo
{
    public DatabaseInfo()
        : this(null)
    {
    }

    public DatabaseInfo(string name)
    {
        Name = name;
    }

    public string Name { get; }

    private bool Equals(IDatabaseInfo other)
    {
        return Name == other.Name;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((DatabaseInfo)obj);
    }

    public override int GetHashCode()
    {
        return Name != null ? Name.GetHashCode() : 0;
    }
}
