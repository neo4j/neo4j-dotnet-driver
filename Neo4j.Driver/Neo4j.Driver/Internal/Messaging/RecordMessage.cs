using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.Messaging
{
    class RecordMessage : IMessage
    {
        private dynamic[] fields;

        public RecordMessage(dynamic[] fields)
        {
            this.fields = fields;
        }

        public override string ToString()
        {
            return $"RECORD {fields.ValueToString()}";
        }
    }
}
