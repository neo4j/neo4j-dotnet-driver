// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

#nullable enable

using System;
using System.Net;
using System.Net.Http;
using Neo4j.Driver.Internal.QueryApi;

namespace Neo4j.Driver.Tests.Internal.QueryApi;

/// <summary>Shared constants and factory methods for Query API unit tests.</summary>
internal static class QueryApiTestHelpers
{
    internal static readonly Uri BaseUri = new("https://localhost:7474");
    internal static QueryApiUrlBuilder UrlBuilder => new(BaseUri);

    private static readonly QueryApiJsonSerializer JsonSerializer = new();

    /// <summary>Builds a 202 Accepted response with a JSON body serialized using the production options.</summary>
    internal static HttpResponseMessage AcceptedWith(object body)
    {
        return new HttpResponseMessage(HttpStatusCode.Accepted) { Content = JsonSerializer.Serialize(body) };
    }

    internal static HttpResponseMessage Accepted()
    {
        return new HttpResponseMessage(HttpStatusCode.Accepted);
    }

    /// <summary>Builds a 200 OK response with a JSON body — for discovery endpoint tests.</summary>
    internal static HttpResponseMessage OkWith(object body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonSerializer.Serialize(body) };
    }

    internal static HttpResponseMessage Unauthorized(
        string code = "Neo.ClientError.Security.Unauthorized",
        string message = "No authentication was supplied.")
    {
        return new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = JsonSerializer.Serialize(new { errors = new[] { new { code, message } } })
        };
    }
}
