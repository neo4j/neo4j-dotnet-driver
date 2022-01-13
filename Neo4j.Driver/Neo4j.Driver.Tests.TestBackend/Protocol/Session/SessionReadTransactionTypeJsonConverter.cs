using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class SessionReadTransactionTypeJsonConverter : JsonConverter<SessionReadTransaction.SessionReadTransactionType>
	{
		public override void WriteJson(JsonWriter writer, SessionReadTransaction.SessionReadTransactionType value, JsonSerializer serializer) =>
			throw new NotImplementedException();

		public override SessionReadTransaction.SessionReadTransactionType ReadJson(JsonReader reader, Type objectType,
			SessionReadTransaction.SessionReadTransactionType existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var json = JObject.Load(reader);
			return new SessionReadTransaction.SessionReadTransactionType
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