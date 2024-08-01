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

using System.Runtime.Serialization;

namespace Neo4j.Driver;

/// <summary>
/// The exception that is thrown when calling an operation in the driver which uses a server feature that is not
/// available on the connected server version.
/// </summary>
[DataContract]
public class UnsupportedFeatureException : ClientException
{
    /// <inheritdoc />
    public override bool IsRetriable => false;
    
    /// <summary>
    /// Creates a new <see cref="UnsupportedFeatureException"/> with an error message.
    /// </summary>
    /// <param name="message">The error message</param>
    internal UnsupportedFeatureException(string message) : base(message)
    {
    }
}
