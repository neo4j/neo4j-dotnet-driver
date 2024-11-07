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

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Neo4j.Driver;

/// <summary>
/// Provides a client certificate to the driver for mutual TLS.
/// <para/>
/// The driver will call <see cref="GetCertificateAsync"/> to get the latest certificate to use for new connections.
/// <para/>
/// The certificate is only used as a second factor for authentication authenticating the client.
/// The DBMS user still needs to authenticate with an authentication token.
/// <para/>
/// All implementations of this interface must be thread-safe and non-blocking for caller threads.
/// For instance, IO operations must not be done on the calling thread.
/// <para/>
/// Note that the work done in the methods of this interface count towards the connectionAcquisition.
/// Should fetching the certificate be particularly slow, it might be necessary to increase the timeout.
/// </summary>
public interface IClientCertificateProvider
{
    /// <summary>
    /// Returns the certificate to use for new connections. Must be thread-safe. The driver will call this
    /// method every time it makes a new connection.
    /// </summary>
    /// <returns>The certificate to use for new connections.</returns>
    /// <remarks>
    /// If a new certificate is returned, existing connections using the previous certificate will not be
    /// affected. The new certificate will only be used for new connections.
    /// <para/>
    /// The work done in this method counts towards the connectionAcquisitionTimeout. Should fetching the
    /// certificate be particularly slow, it might be necessary to increase the timeout.
    /// <para/>
    /// This method must be thread safe.
    /// </remarks>
    ValueTask<X509Certificate> GetCertificateAsync();
}
