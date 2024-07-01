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

using System;
using System.IO;
using System.Net.Security;

namespace Neo4j.Driver;

/// <summary>
/// Defines a method that negotiates a TLS connection.
/// </summary>
public interface ITlsNegotiator
{
    /// <summary>
    /// Return a secured stream for the given URI and stream.
    /// </summary>
    /// <param name="uri">The URI being connected to.</param>
    /// <param name="stream">The stream to negotiate the TLS connection on.</param>
    /// <returns></returns>
    SslStream NegotiateTls(Uri uri, Stream stream);
}

/// <summary>
/// A delegate that negotiates a TLS connection.
/// </summary>
public delegate SslStream NegotiateTlsDelegate(Uri uri, Stream stream);
