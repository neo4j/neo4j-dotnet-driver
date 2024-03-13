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

namespace Neo4j.Driver.Preview.Auth;

/// <summary>
/// Contains methods for Auth features that are in preview.
/// </summary>
public static class AuthConfigBuilderExtensions
{
    /// <summary>
    /// Sets the client certificate provider to be used for when creating new connections.
    /// <para/>
    /// This method is in preview and may be removed or changed in the future.
    /// </summary>
    /// <param name="builder">The <see cref="ConfigBuilder"/> to set the client certificate provider on.</param>
    /// <param name="provider">The client certificate provider to use.</param>
    /// <returns>The <see cref="ConfigBuilder"/> for chaining.</returns>
    public static ConfigBuilder WithClientCertificateProvider(
        this ConfigBuilder builder,
        IClientCertificateProvider provider)
    {
        return builder.WithClientCertificateProvider(provider);
    }
}
