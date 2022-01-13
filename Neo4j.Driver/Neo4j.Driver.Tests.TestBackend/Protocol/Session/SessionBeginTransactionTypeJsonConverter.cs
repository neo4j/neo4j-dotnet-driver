using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class SessionBeginTransactionTypeJsonConverter : JsonConverter<SessionBeginTransaction.SessionBeginTransactionType>
	{
		public override void WriteJson(JsonWriter writer, SessionBeginTransaction.SessionBeginTransactionType value, JsonSerializer serializer) =>
			throw new NotImplementedException();

		public override SessionBeginTransaction.SessionBeginTransactionType ReadJson(JsonReader reader, Type objectType,
			SessionBeginTransaction.SessionBeginTransactionType existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var json = JObject.Load(reader);
			return new SessionBeginTransaction.SessionBeginTransactionType
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