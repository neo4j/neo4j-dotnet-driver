using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Internal.Result;

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