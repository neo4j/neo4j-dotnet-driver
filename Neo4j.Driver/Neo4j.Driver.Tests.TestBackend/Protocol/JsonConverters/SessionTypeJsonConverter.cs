using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SessionTypeJsonConverter : BaseSessionTypeJsonConverter<SessionRun.SessionRunType>
	{
		public override void WriteJson(JsonWriter writer, SessionRun.SessionRunType value, JsonSerializer serializer) =>
			throw new NotImplementedException();

		public override SessionRun.SessionRunType ReadJson(JsonReader reader, Type objectType,
			SessionRun.SessionRunType existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var json = JObject.Load(reader);
			var sessionType = new SessionRun.SessionRunType
			{
				cypher = json["cypher"]?.Value<string>(),
                parameters = JsonCypherParameterParser.ParseParameters(json["params"])
            };
            SetBaseValues(json, sessionType);
            return sessionType;
        }
    }
}