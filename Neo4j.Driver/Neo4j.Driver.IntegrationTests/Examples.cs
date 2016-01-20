using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    public class Examples
    {
        private readonly ITestOutputHelper output;
        
        public Examples(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void RunExample()
        {
            //tag::minimum-snippet[]
            Driver driver = GraphDatabase.Driver("bolt://localhost:7687");
            ISession session = driver.Session();

            session.Run("CREATE (neo:Person {name:'Neo', age:23})");

            ResultCursor result = session.Run("MATCH (p:Person) WHERE p.name = 'Neo' RETURN p.age");
            while (result.Next())
            {
                output.WriteLine($"Neo is {result.Value(("p.age"))} years old.");
            }

            session.Dispose();
            driver.Dispose();
            //end::minimum-snippet[]
        }
    }
}
