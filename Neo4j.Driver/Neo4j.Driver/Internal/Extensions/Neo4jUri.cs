// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.Internal;

internal static class Neo4jUri
{
    public const int DefaultBoltPort = 7687;
    
    public static bool IsSimpleUriScheme(Uri uri)
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

    public static bool IsRoutingUri(Uri uri)
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

    public static EncryptionManager ParseUriSchemeToEncryptionManager(Uri uri, ILogger logger)
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

    public static Uri ParseBoltUri(Uri uri, int defaultPort)
    {
        var port = defaultPort;
        if (uri.Port != -1)
        {
            port = uri.Port;
        }

        var builder = new UriBuilder(uri.Scheme, uri.Host, port);
        return builder.Uri;
    }

    public static IDictionary<string, string> ParseRoutingContext(Uri uri, int defaultPort)
    {
        if (!IsRoutingUri(uri))
        {
            return new Dictionary<string, string>();
        }

        var query = uri.Query;
        IDictionary<string, string> context = new Dictionary<string, string>();

        //First add in the address and port the client used to contact the server
        const string addressKey = "address";
        var addressValue = uri.Port == -1 ? uri.Authority + ":" + defaultPort : uri.Authority;
        context.Add(addressKey, addressValue);

        foreach (var pair in query.Split(new[] { '?', '&' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var keyValue = pair.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (keyValue.Length != 2)
            {
                throw new ArgumentException($"Invalid parameters: '{pair}' in URI '{uri}'.", pair);
            }

            var key = keyValue[0];
            var value = keyValue[1];
            if (context.ContainsKey(key))
            {
                if (key == addressKey)
                {
                    throw new ArgumentException($"The key {addressKey} is reserved for routing context.");
                }

                throw new ArgumentException($"Duplicated query parameters with key '{key}' in URI '{uri}'.", key);
            }

            context.Add(key, value);
        }

        return context;
    }

    internal static void EnsureNoRoutingContextOnBolt(Uri uri)
    {
        if (!IsRoutingUri(uri) && !string.IsNullOrEmpty(uri.Query))
        {
            throw new ArgumentException($"Routing context are not supported with scheme 'bolt'. Given URI: '{uri}'");
        }
    }

    public static Uri BoltRoutingUri(string address)
    {
        var builder = new UriBuilder("neo4j://" + address);
        if (builder.Port == -1)
        {
            builder.Port = DefaultBoltPort;
        }
        return builder.Uri;
    }
}
