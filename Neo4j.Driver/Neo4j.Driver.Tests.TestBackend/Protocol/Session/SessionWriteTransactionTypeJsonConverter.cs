using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class SessionWriteTransactionTypeJsonConverter : JsonConverter<SessionWriteTransaction.SessionWriteTransactionType>
	{
		public override void WriteJson(JsonWriter writer, SessionWriteTransaction.SessionWriteTransactionType value, JsonSerializer serializer) =>
			throw new NotImplementedException();

		public override SessionWriteTransaction.SessionWriteTransactionType ReadJson(JsonReader reader, Type objectType,
			SessionWriteTransaction.SessionWriteTransactionType existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var json = JObject.Load(reader);
			return new SessionWriteTransaction.SessionWriteTransactionType
			{
				sessionId = json["sessionId"]?.Value<string>(),
				txMeta = json["txMeta"]?.ToObject<Dictionary<string, object>>()
							?? new Dictionary<string, object>(),
				timeout = json["timeout"]?.Value<int?>(),
				TimeoutSet = json.ContainsKey("timeout")
			};
		}
	}
}