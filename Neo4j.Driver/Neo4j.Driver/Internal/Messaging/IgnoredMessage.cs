namespace Neo4j.Driver
{
    public class IgnoredMessage : IMessage
    {
        public override string ToString()
        {
            return "IGNORED";
        }
    }
}