using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class SeessionRunTypeJsonConverter : JsonConverter<SessionRun.SessionRunType>
	{
		public override void WriteJson(JsonWriter writer, SessionRun.SessionRunType value, JsonSerializer serializer) =>
			throw new NotImplementedException();

		public override SessionRun.SessionRunType ReadJson(JsonReader reader, Type objectType,
			SessionRun.SessionRunType existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var json = JObject.Load(reader);
			return new SessionRun.SessionRunType
			{
				sessionId = json["sessionId"]?.Value<string>(),
				cypher = json["cypher"]?.Value<string>(),
				parameters = json["params"]?.ToObject<Dictionary<string, CypherToNativeObject>>() 
								?? new Dictionary<string, CypherToNativeObject>(),
				txMeta = json["txMeta"]?.ToObject<Dictionary<string, object>>() 
							?? new Dictionary<string, object>(),
				timeout = json["timeout"]?.Value<int?>(),
				TimeoutSet = json.ContainsKey("timeout")
			};
		}
	}
}