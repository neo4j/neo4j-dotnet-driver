using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver
{
    public enum StatementType
    {
        ReadOnly,
        ReadWrite,
        WriteOnly,
        SchemaWrite
    }

    public interface IResultSummary
    {

        ///
        /// Return statement that has been executed 
        Statement Statement { get; }

        ///
        /// Return update statistics for the statement
        ///
        IUpdateStatistics UpdateStatistics { get; }

        ///
        /// Return type of statement that has been executed
        ///
        StatementType StatementType { get; }

        ///
        /// Return true if the result contained a statement plan, i.e. is the summary of a Cypher "PROFILE" or "EXPLAIN" statement
        ///
        bool HasPlan { get; }

        ///
        /// Return true if the result contained profiling information, i.e. is the summary of a Cypher "PROFILE" statement
        ///
        bool HasProfile { get; }

        ///
        /// This describes how the database will execute your statement.
        ///
        /// Return statement plan for the executed statement if available, otherwise null
        ///
        IPlan Plan { get; }

        ///
        /// This describes how the database did execute your statement.
        ///
        /// If the statement you executed {@link #hasProfile() was profiled}, the statement plan will contain detailed
        /// information about what each step of the plan did. That more in-depth version of the statement plan becomes
        /// available here.
        ///
        /// Return profiled statement plan for the executed statement if available, otherwise null
        ///
        IProfiledPlan Profile { get; }

        ///
        /// A list of notifications that might arise when executing the statement.
        /// Notifications can be warnings about problematic statements or other valuable information that can be presented
        /// in a client.
        ///
        /// Unlike failures or errors, notifications do not affect the execution of a statement.
        ///
        /// Return a list of notifications produced while executing the statement. The list will be empty if no
        /// notifications produced while executing the statement.
        ///
        IList<INotification> Notifications { get; }
    }
}
