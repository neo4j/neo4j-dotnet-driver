using System;
using System.Collections.Generic;
using Neo4j.Driver.Internal.Messaging.V4_4;

namespace Neo4j.Driver.Internal.IO.MessageSerializers.V4_4;

internal sealed class RouteMessageSerializer : WriteOnlySerializer
{
    internal static RouteMessageSerializer Instance = new();
    
    private static readonly Type[] MessageSerializer = {typeof(RouteMessage)};
    public override IEnumerable<Type> WritableTypes => MessageSerializer;

    public override void Serialize(PackStreamWriter writer, object value)
    {
        if (value is not RouteMessage msg)
            throw new ArgumentOutOfRangeException(
                $"Encountered {value?.GetType().Name} where {nameof(RouteMessage)} was expected.");

        writer.WriteStructHeader(3, MessageFormat.MsgRoute);
        writer.WriteDictionary(msg.Routing);
        writer.WriteList(msg.Bookmarks.Values);
        writer.WriteDictionary(msg.DatabaseContext);
    }
}