using System.Collections.Generic;

namespace Neo4j.Driver
{
    public interface IProfiledPlan:IPlan
    {
        ///
        /// Return the number of times this part of the plan touched the underlying data stores
        ///
        long DbHits { get; }

        ///
        ///Return the number of records this part of the plan produced
        ///
        long Records { get; }

        new IList<IProfiledPlan> Children();
    }
}