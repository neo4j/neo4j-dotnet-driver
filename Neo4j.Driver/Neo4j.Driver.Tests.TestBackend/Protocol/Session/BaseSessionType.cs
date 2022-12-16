using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal abstract class BaseSessionType
    {
        public string sessionId { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        [JsonConverter(typeof(QueryParameterConverter))]
        public Dictionary<string, CypherToNativeObject> txMeta { get; set; } = new();

        [JsonProperty(Required = Required.AllowNull)]
        public int? timeout { get; set; }

        [JsonIgnore]
        public bool TimeoutSet { get; set; }

        public TransactionConfigBuilder ConfigureTxTimeout(TransactionConfigBuilder configBuilder)
        {
            try
            {
                if (TimeoutSet)
                {
                    var timeout = this.timeout.HasValue
                        ? TimeSpan.FromMilliseconds(this.timeout.Value)
                        : default(TimeSpan?);

                    configBuilder.WithTimeout(timeout);
                }
            }
            catch (ArgumentOutOfRangeException e) when ((timeout ?? 0) < 0 && e.ParamName == "value")
            {
                throw new DriverExceptionWrapper(e);
            }

            return configBuilder;
        }

        public TransactionConfigBuilder ConfigureTxMetadata(TransactionConfigBuilder configBuilder)
        {
            if (txMeta.Count > 0) configBuilder.WithMetadata(CypherToNativeObject.ConvertDictionaryToNative(txMeta));
            return configBuilder;
        }

        public void TransactionConfig(TransactionConfigBuilder configBuilder)
        {
            ConfigureTxMetadata(ConfigureTxTimeout(configBuilder));
        }
    }
}
