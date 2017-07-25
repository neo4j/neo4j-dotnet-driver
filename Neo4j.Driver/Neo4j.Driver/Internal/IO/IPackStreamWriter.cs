using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Neo4j.Driver.Internal.IO
{
    internal interface IPackStreamWriter
    {

        void Write(object value);

    }
}
