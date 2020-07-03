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
        public static bool IsSimpleUriScheme(this Uri uri)
        {
            var scheme = uri.Scheme.ToLower();
            switch (scheme)
            {
                case "bolt+s":
                case "bolt+ssc":
                case "neo4j+s":
                case "neo4j+ssc":
                    return false;
                case "bolt":
                case "neo4j":
                    return true;
                default:
                    throw new NotSupportedException($"Unsupported URI scheme: {scheme}");
            }
        }
        public static bool IsRoutingUri(this Uri uri)
        {
            var scheme = uri.Scheme.ToLower();
            switch (scheme)
            {
                case "bolt":
                case "bolt+s":
                case "bolt+ssc":
                    return false;
                case "neo4j":
                case "neo4j+s":
                case "neo4j+ssc":
                    return true;
                default:
                    throw new NotSupportedException($"Unsupported URI scheme: {scheme}");
            }
        }

        public static EncryptionManager ParseUriSchemeToEncryptionManager(this Uri uri, ILogger logger)
        {
            var scheme = uri.Scheme.ToLower();
            switch (scheme)
            {
                case "bolt":
                case "neo4j":
                    // no encryption, no trust
                    return new EncryptionManager(false, null);
                case "bolt+s":
                case "neo4j+s":
                    // encryption with chain trust
                    return new EncryptionManager(true, EncryptionManager.CreateSecureTrustManager(logger));
                case "bolt+ssc":
                case "neo4j+ssc":
                    return new EncryptionManager(true, EncryptionManager.CreateInsecureTrustManager(logger));
                default:
                    throw new NotSupportedException($"Unsupported URI scheme: {scheme}");
            }
        }

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

        public static IDictionary<string, string> ParseRoutingContext(this Uri uri, int defaultPort)
        {
            if(!uri.IsRoutingUri())
                return new Dictionary<string, string>();

            string query = uri.Query;
            IDictionary<string, string> context = new Dictionary<string, string>();

            //First add in the address and port the client used to contact the server
            const string addressKey = "address";
            string addressValue = (uri.Port == -1) ? uri.Authority + ":" + defaultPort : uri.Authority;
            context.Add(addressKey, addressValue);

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
                    if (key == addressKey)
                        throw new ArgumentException($"The key {addressKey} is reserved for routing context.");
                    else
                        throw new ArgumentException($"Duplicated query parameters with key '{key}' in URI '{uri}'.", key);
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
