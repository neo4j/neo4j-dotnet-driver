// Copyright (c) 2002-2020 "Neo4j,"
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

using Neo4j.Driver.Internal.Messaging.V3;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4j.Driver.Internal.Messaging.V4_3
{
	internal class RouteMessage : IRequestMessage
	{	
        public IDictionary<string, string> Routing { get; set; }
        public string DatabaseParam { get; set; }


        public RouteMessage(IDictionary<string, string> routingContext, string db)
		{
            Routing = routingContext;
            DatabaseParam = db;            
		}

		public override string ToString()   
		{
            string message = "{";

            foreach(var data in Routing)
			{
                message += $" \'{data.Key}\':\'{data.Value}\'";
            }

            message += "}";

            message += (DatabaseParam != null) ? " \'" + DatabaseParam + "\'" : "";

            return message;
        }
	}
}
