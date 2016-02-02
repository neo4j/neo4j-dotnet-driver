using System.Collections.Generic;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver
{
    internal class SuccessMessage : IMessage
    {
        private IDictionary<string, object> meta;

        public SuccessMessage(IDictionary<string, object> meta)
        {
            this.meta = meta;
        }

        public override string ToString()
        {
            return $"SUCCESS {meta.ValueToString()}";
        }
    }
}