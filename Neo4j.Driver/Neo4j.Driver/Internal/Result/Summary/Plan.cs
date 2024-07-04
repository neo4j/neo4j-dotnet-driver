using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Result;

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