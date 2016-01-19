using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Extensions
{
    public static class Extensions
    {
        public static T[] DequeueToArray<T>(this Queue<T> queue, int length)
        {
            var output = new T[length];
            for (int i = 0; i < length; i++)
            {
                output[i] = queue.Dequeue();
            }
            return output;
        }
    }
}
