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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal
{
    internal static class NetworkExtensions
    {
        
        public static Uri ParseBoltUri(this Uri uri, int defaultPort)
        {
            var port = defaultPort;
            if (uri.Port != -1)
            {
                port = uri.Port;
            }
            var builder = new UriBuilder(uri.Scheme, uri.Host, port);
            return builder.Uri;
        }

        public static IDictionary<string, string> ParseRoutingContext(this Uri uri)
        {
            string query = uri.Query;
            if (string.IsNullOrEmpty(query))
            {
                return new Dictionary<string, string>();
            }

            IDictionary<string, string> context = new Dictionary<string, string>();
            foreach (var pair in query.Split(new[] {'?', '&'}, StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = pair.Split(new []{'='}, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length != 2)
                {
                    throw new ArgumentException(
                        $"Invalid parameters: '{pair}' in URI '{uri}'.", pair);
                }

                var key = keyValue[0];
                var value = keyValue[1];
                if (context.ContainsKey(key))
                {
                    throw new ArgumentException(
                        $"Duplicated query parameters with key '{key}' in URI '{uri}'.", key);
                }
                context.Add(key, value);
            }
            return context;
        }

        public static bool IsTimeoutDetectionEnabled(this TimeSpan timeout)
        {
            return timeout.TotalMilliseconds >= 0;
        }

        public static bool IsTimeoutDetectionDisabled(this TimeSpan timeout)
        {
            return timeout.TotalMilliseconds < 0;
        }
    }
}
