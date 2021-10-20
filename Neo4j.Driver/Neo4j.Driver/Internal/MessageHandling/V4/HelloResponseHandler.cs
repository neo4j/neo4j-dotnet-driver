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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.MessageHandling.V4
{
    internal class HelloResponseHandler : V3.HelloResponseHandler
	{
		readonly BoltProtocolVersion MinVersion = new BoltProtocolVersion(4, 0);
		protected BoltProtocolVersion _version;

		protected BoltProtocolVersion Version
		{
			get
			{
				return _version;
			}
			set
			{
				_version = value ?? throw new ArgumentNullException("Attempting to create a HelloResponseHandler v{MinVersion.ToString()} with a null BoltProtocolVersion object");
				if (Version < MinVersion)
					throw new ArgumentOutOfRangeException($"Attempting to initialise a v{MinVersion.ToString()} HelloResponseHandler with a protocol version less than {MinVersion.ToString()}");
			}
		}

		public HelloResponseHandler(IConnection connection, BoltProtocolVersion version) : base(connection)
        {
			//Add version specific Metadata collectors here...
			Version = version;
		}

		public override void OnSuccess(IDictionary<string, object> metadata)
        {
			base.OnSuccess(metadata);

			//Version specific handling goes here...
        }

		protected override void UpdateVersion()
		{
			_connection.UpdateVersion(new ServerVersion(Version.MajorVersion, Version.MinorVersion, 0));
		}
    }
}