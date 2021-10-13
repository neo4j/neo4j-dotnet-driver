// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading;
using System.Collections.Generic;

namespace Neo4j.Driver.Internal.MessageHandling.Metadata
{
	class ConfigurationHintsCollector : IMetadataCollector<Dictionary<string, object>>
	{
		internal const string ConfigHintsKey = "hints";		

		object IMetadataCollector.Collected => Collected;

		public Dictionary<string, object> Collected { get; private set; }		


		public void Collect(IDictionary<string, object> metadata)
		{
			if (metadata is not null && metadata.TryGetValue(ConfigHintsKey, out var configHintsObject))
			{
				if (configHintsObject is Dictionary<string, object> hints)
				{
					Collected = hints;					
				}
				else
				{
					throw new ProtocolException(
					   $"Expected '{ConfigHintsKey}' metadata to be of type 'Dictionary<string, object>', but got '{configHintsObject?.GetType().Name}'.");
				}
			}			
		}
	}



	interface IConfigHint<out TType>
	{
		TType Get { get; }
	}

	class ConfigHintRecvTimeout : IConfigHint<int>
	{
		internal const string ConnectionRecvTimeoutSeconds = "connection.recv_timeout_seconds";

		public int Get { get; } = -1; 

		private ConfigHintRecvTimeout() { }	//Private default constructor.

		public ConfigHintRecvTimeout(Dictionary<string, object> configHintsObject)
		{
			if (configHintsObject is null)
				return;

			if (configHintsObject.TryGetValue(ConnectionRecvTimeoutSeconds, out var recvTimeoutSecs))
			{
				Get = Convert.ToInt32(recvTimeoutSecs);				
			}
		}
	}
}
