using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Result
{
    internal class RecordSet : IRecordSet
    {
        private readonly Func<bool> _atEnd;
        internal int Position = 0;
        private readonly IList<IRecord> _records;

        public RecordSet(IList<IRecord> records, Func<bool> atEnd)
        {
            _records = records;
            _atEnd = atEnd;
        }

        public bool AtEnd => _atEnd();

        public IEnumerable<IRecord> Records()
        {
            while (!AtEnd || Position <= _records.Count)
            {
                while (Position == _records.Count)
                {
                    Task.Delay(50).Wait();
                    if (AtEnd && Position == _records.Count)
                        yield break;
                }

                yield return _records[Position++];
//                Position++;
            }
        }

        public IRecord Peek()
        {
            while (Position >= _records.Count) // Peeking record not received
            {
                if (AtEnd && Position >= _records.Count)
                {
                    return null;
                }

                Task.Delay(50).Wait();
            }

            return _records[Position];
        }
    }
}
