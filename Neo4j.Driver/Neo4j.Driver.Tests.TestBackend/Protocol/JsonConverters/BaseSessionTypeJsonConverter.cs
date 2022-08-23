using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class BaseSessionTypeJsonConverter<T> : JsonConverter<T> where T: BaseSessionType, new()
    {
        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override T ReadJson(JsonReader reader, Type objectType,
            T existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var json = JObject.Load(reader);
            var sessionSettings = new T();
            SetBaseValues(json, sessionSettings);
            return sessionSettings;
        }

        protected void SetBaseValues(JObject jsonObject, T baseSession)
        {
            baseSession.sessionId = jsonObject["sessionId"]?.Value<string>();
            baseSession.txMeta = jsonObject["txMeta"]?.ToObject<Dictionary<string, object>>()
                                 ?? new Dictionary<string, object>();
            baseSession.timeout = jsonObject["timeout"]?.Value<int?>();
            baseSession.TimeoutSet = jsonObject.ContainsKey("timeout");
        }
    }
}