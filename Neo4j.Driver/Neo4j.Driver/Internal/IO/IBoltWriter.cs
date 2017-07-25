using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.IO
{
    internal interface IBoltWriter
    {

        void Write(IRequestMessage message);

        void Flush();

        Task FlushAsync();

    }
}
