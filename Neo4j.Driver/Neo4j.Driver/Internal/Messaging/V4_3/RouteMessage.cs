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

namespace Neo4j.Driver.Internal.Messaging.V4_3
{
	internal class RouteMessage : IRequestMessage
	{

		public IDictionary<string, string> Routing { get;}
		public Bookmarks Bookmarks { get; }
		public string DatabaseParam { get; }


		public RouteMessage(IDictionary<string, string> routingContext, Bookmarks bookmarks, string db)
		{
            Routing = routingContext ?? new Dictionary<string,string>();
			Bookmarks = bookmarks ?? Bookmarks.From(Array.Empty<string>());
            DatabaseParam = db;            
		}

		public override string ToString()   
		{
			string message = "ROUTE {";

			foreach(var data in Routing)
			{
                message += $" \'{data.Key}\':\'{data.Value}\'";
			}

            message += " } ";

			message += (Bookmarks is not null && Bookmarks.Values.Length > 0) 
				? "{ bookmarks, " + Bookmarks.Values.ToContentString() + " }" 
				: Array.Empty<string>().ToContentString();
			
			message += (!string.IsNullOrEmpty(DatabaseParam)) ? " \'" + DatabaseParam + "\'" : " None";

            return message;
        }
	}
}
