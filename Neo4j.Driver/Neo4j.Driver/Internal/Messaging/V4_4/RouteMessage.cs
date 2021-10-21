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

namespace Neo4j.Driver.Internal.Messaging.V4_4
{
	internal class RouteMessage : IRequestMessage
	{
		private const string DBNameKey = "db";
		private const string ImpersonatedUserKey = "imp_user";

		public IDictionary<string, string> Routing { get; }
		public Bookmark Bookmark { get; }
		public IDictionary<string, string> DatabaseContext { get; }


		public RouteMessage(IDictionary<string, string> routingContext, Bookmark bookmark, string databaseName, string impersonatedUser)
		{
			Routing = routingContext ?? new Dictionary<string, string>();
			Bookmark = bookmark ?? Bookmark.From(Array.Empty<string>());
			DatabaseContext = new Dictionary<string, string>() { { DBNameKey, databaseName },
																 { ImpersonatedUserKey, impersonatedUser} };
		}

		public override string ToString()
		{
			string message = "ROUTE {";

			foreach (var data in Routing)
			{
				message += $" \'{data.Key}\':\'{data.Value}\'";
			}

			message += " } ";

			message += (Bookmark is not null && Bookmark.Values.Length > 0)
				? "{ bookmarks, " + Bookmark.Values.ToContentString() + " }"
				: Array.Empty<string>().ToContentString();

			message += " {";

			foreach (var data in DatabaseContext)
			{
				message += $" \'{data.Key}\':\'{data.Value}\'";
			}

			message += " } ";

			return message;
		}
	}
}