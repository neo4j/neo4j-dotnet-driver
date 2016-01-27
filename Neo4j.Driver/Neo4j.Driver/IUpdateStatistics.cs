namespace Neo4j.Driver
{
    public interface IUpdateStatistics
    {
        /// 
        /// Whether there were any updates at all, eg. any of the counters are greater than 0.
        /// Return true if the statement made any updates
        /// 
        bool ContainsUpdates { get; }

        /// 
        /// Return number of nodes created.
        /// 
        int NodesCreated { get; }

        /// 
        /// Return number of nodes deleted.
        /// 
        int NodesDeleted { get; }

        /// 
        /// Return number of relationships created.
        /// 
        int RelationshipsCreated { get; }

        /// 
        /// Return number of relationships deleted.
        /// 
        int RelationshipsDeleted { get; }

        /// 
        /// Return number of properties (on both nodes and relationships) set.
        /// 
        int PropertiesSet { get; }

        /// 
        /// Return number of labels added to nodes.
        /// 
        int LabelsAdded { get; }

        /// 
        /// Return number of labels removed from nodes.
        /// 
        int LabelsRemoved { get; }

        /// 
        /// Return number of indexes added to the schema.
        /// 
        int IndexesAdded { get; }

        /// 
        /// Return number of indexes removed from the schema.
        /// 
        int IndexesRemoved { get; }

        /// 
        /// Return number of constraints added to the schema.
        /// 
        int ConstraintsAdded { get; }

        /// 
        /// Return number of constraints removed from the schema.
        /// 
        int ConstraintsRemoved { get; }
    }
}