using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests
{
    public static class TestHelper
    {
        public static byte[] StringToByteArray(string hex)
        {
            hex = hex.Replace(" ", "").Replace(Environment.NewLine, "");
            return Enumerable.Range(0, hex.Length)
                 .Where(x => x % 2 == 0)
                 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                 .ToArray();
        }
    }
}
