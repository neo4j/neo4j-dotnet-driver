using System.Collections.Generic;

namespace Neo4j.Driver.Internal.result
{
    internal class SummaryBuilder
    {
        public Statement Statement { private get; set; }
        public StatementType StatementType { private get; set; }
        public IUpdateStatistics UpdateStatistics { private get; set; }
        public IPlan Plan { private get; set; }
        public IProfiledPlan Profile { private get; set; }
        public IList<INotification> Notifications { private get; set; }

        public SummaryBuilder(Statement statement)
        {
            Statement = statement;
        }

        public IResultSummary Build()
        {
            return new ResultSumamry(this);
        }

        private class ResultSumamry:IResultSummary
        {
            public ResultSumamry(SummaryBuilder builder)
            {
                Statement = builder.Statement;
                StatementType = builder.StatementType;
                UpdateStatistics = builder.UpdateStatistics;
                Plan = builder.Plan;
                Profile = builder.Profile;
                Notifications = builder.Notifications;
            }

            public Statement Statement { get; }
            public IUpdateStatistics UpdateStatistics { get; }
            public StatementType StatementType { get; }
            public bool HasPlan => Plan != null;
            public bool HasProfile => Profile != null;
            public IPlan Plan { get; }
            public IProfiledPlan Profile { get; }
            public IList<INotification> Notifications { get; }
        }
    }

    public class Plan : IPlan
    {
    
        public Plan(string operationType, Dictionary<string, object> args, IList<string> identifiers, List<IPlan> childPlans)
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
    }

    public class UpdateStatistics : IUpdateStatistics
    {
        public bool ContainsUpdates => (
            IsPositive(NodesCreated)
            || IsPositive(NodesDeleted)
            || IsPositive(RelationshipsCreated)
            || IsPositive(RelationshipsDeleted)
            || IsPositive(PropertiesSet)
            || IsPositive(LabelsAdded)
            || IsPositive(LabelsRemoved)
            || IsPositive(IndexesAdded)
            || IsPositive(IndexesRemoved)
            || IsPositive(ConstraintsAdded)
            || IsPositive(ConstraintsRemoved));
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

        public UpdateStatistics(int nodesCreated, int nodesDeleted, int relationshipsCreated, int relationshipsDeleted, int propertiesSet, int labelsAdded, int labelsRemoved, int indexesAdded, int indexesRemoved, int constraintsAdded, int constraintsRemoved)
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
        }

        private bool IsPositive(int value)
        {
            return value > 0;
        }
    }

    public class Notification : INotification
    {
        public string Code { get; }
        public string Title { get; }
        public string Description { get; }
        public IInputPosition Position { get; }

        public Notification(string code, string title, string description, IInputPosition position)
        {
            Code = code;
            Title = title;
            Description = description;
            Position = position;
        }
    }

    public class InputPosition : IInputPosition
    {
        public int Offset { get; }
        public int Line { get; }
        public int Column { get; }

        public InputPosition(int offset, int line, int column)
        {
            Offset = offset;
            Line = line;
            Column = column;
        }
    }
}