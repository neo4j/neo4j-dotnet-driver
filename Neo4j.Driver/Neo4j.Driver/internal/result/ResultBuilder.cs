using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal.result
{
    public class ResultBuilder
    {
        //handles success

        void keys(String[] names)
        {
            
        }

        void record(object[] fields)
        {
            
        }

        public Result Build()
        {
            throw new NotImplementedException();
        }

        public void CollectMeta(IDictionary<string, object> meta)
        {
            if (meta == null)
                return;
            //
            foreach (var item in meta)
            {
                Debug.WriteLine("{0} -> {1}", item.Key, item.Value);
            }
        }
    }
}
