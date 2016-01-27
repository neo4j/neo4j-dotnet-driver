using System.Collections.Generic;

namespace Neo4j.Driver
{
    public interface IPlan
    {
        ///
        /// Return the operation this plan is performing.
        ///
        string OperatorType { get; }

        ///
        /// Many {@link #operatorType() operators} have arguments defining their specific behavior. This map contains
        /// those arguments.
        ///
        /// Return the arguments for the {@link #operatorType() operator} used.
        ///
        IDictionary<string, object> Arguments { get; }

        ///
        /// Identifiers used by this part of the plan. These can be both identifiers introduce by you, or automatically
        /// generated identifiers.
        /// Return a list of identifiers used by this plan.
        ///
        IList<string> Identifiers { get; }

        ///
        /// As noted in the class-level javadoc, a plan is a tree, where each child is another plan. The children are where
        /// this part of the plan gets its input records - unless this is an {@link #operatorType() operator} that introduces
        /// new records on its own.
        /// Return zero or more child plans.
        ///
        IList<IPlan> Children { get; }
    }
}