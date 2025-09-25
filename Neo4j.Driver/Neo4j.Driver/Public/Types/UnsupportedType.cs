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

using System.Dynamic;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver;

/// <summary>
/// Represents a type unknown to the driver, received from the server.
/// This type is used for instance when a newer DBMS produces a result containing a type that the current version of the driver does not yet understand.
///
/// Note that this type may only be received from the server, but cannot be sent to the server (e.g., as a query parameter).
/// 
/// The attributes exposed by this type are meant for displaying and debugging purposes.
/// They may change in future versions of the server, and should not be relied upon for any logic in your application.
/// If your application requires handling this type, you must upgrade your driver to a version that supports it.
/// </summary>
public class UnsupportedType
{
    /// <summary>
    /// Gets the name of the unsupported type as provided by the server.
    /// For example, <c>"UUID"</c> or <c>"Vector"</c>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Returns the minimum required Bolt protocol version that supports this type.
    /// To understand which driver version this corresponds to, refer to the driver's release notes or documentation.
    /// </summary>
    public string MinimumProtocolVersion { get; }

    /// <summary>
    /// Gets an optional message from the server with additional information about the unsupported type.
    /// This may include hints, migration paths, or required configuration options. May be <c>null</c>.
    /// </summary>
    public string Message { get; }

    internal UnsupportedType(string name, int minimumProtocolMajor, int minimumProtocolMinor, string message)
    {
        Name = name;
        MinimumProtocolVersion = $"{minimumProtocolMajor}.{minimumProtocolMinor}";
        Message = message;
    }
}
