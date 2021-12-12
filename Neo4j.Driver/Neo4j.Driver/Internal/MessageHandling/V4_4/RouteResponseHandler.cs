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
using System.Collections.Generic;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.MessageHandling.V4_4
{
	class RouteResponseHandler : MetadataCollectingResponseHandler
	{
		public IDictionary<string, object> RoutingInformation { get; set; }


		public RouteResponseHandler()
		{
			AddMetadata<RoutingTableCollector, IDictionary<string, object>>();
		}

		public override void OnSuccess(IDictionary<string, object> metadata)
		{
			base.OnSuccess(metadata);
			RoutingInformation = GetMetadata<RoutingTableCollector, IDictionary<string, object>>();
		}
	}

}
