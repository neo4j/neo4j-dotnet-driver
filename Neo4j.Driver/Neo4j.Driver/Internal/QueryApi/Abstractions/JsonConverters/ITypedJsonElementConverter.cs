using System.Text.Json;

namespace Neo4j.Driver.Internal.QueryApi.Abstractions.JsonConverters;

internal interface ITypedJsonElementConverter
{
    bool CanConvert(string typeName);
    object Convert(JsonElement jsonElement);
}