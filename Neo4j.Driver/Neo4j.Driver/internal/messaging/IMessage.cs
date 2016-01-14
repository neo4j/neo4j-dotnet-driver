namespace Neo4j.Driver
{
    public interface IMessage
    {
        void Dispatch(IMessageRequestHandler messageRequestHandler);
    }
}