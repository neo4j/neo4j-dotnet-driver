namespace Neo4j.Driver.Internal.Result;

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