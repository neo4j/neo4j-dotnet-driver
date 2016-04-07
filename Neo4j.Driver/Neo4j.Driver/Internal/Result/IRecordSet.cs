using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Result
{
    /// <summary>
    /// A record set represents a set of records where only forward enumeration is possible.
    /// This means that when a record has been visited by enumeration, then it will not be any
    /// future enumerations. It is consumed.
    /// </summary>
    internal interface IRecordSet
    {
        /// <summary>
        /// Has all records been consumed.
        /// </summary>
        bool AtEnd { get; }

        /// <summary>
        /// The position in the forward stream of records.
        /// This is -1 before any records has been consumed.
        /// This is the 0-indexed position of the just consumed record.
        /// When there is no more records, this has the value of number of records (count).
        /// </summary>
        int Position { get; }

        /// <summary>
        /// Peeks a record without consuming. 
        /// If all records has been consumed, this is null
        /// </summary>
        IRecord Peek { get; }

        /// <summary>
        /// Returns an IEnumerable of records.
        /// </summary>
        IEnumerable<IRecord> Records { get; }
    }
}
