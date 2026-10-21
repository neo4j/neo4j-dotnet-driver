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
using System.Text.Json;

namespace Neo4j.Driver.Internal.QueryApi;

/// <summary>
/// Writes the HTTP Query API typed JSON envelope (<c>{"$type":"...", "_value":...}</c>) around a value.
/// </summary>
internal interface IJsonEnvelopeWriter
{
    /// <summary>
    /// Opens a typed envelope on the given writer: writes the opening object, the <c>$type</c> property and the
    /// <c>_value</c> property name. The caller writes the value body, then disposes the returned scope to write
    /// the closing brace. Intended to be used with a <c>using</c> statement.
    /// </summary>
    /// <param name="writer">The writer to emit the envelope to.</param>
    /// <param name="typeDescriptor">The value of the <c>$type</c> property, e.g. <c>"Integer"</c>.</param>
    /// <returns>A scope that closes the envelope when disposed.</returns>
    IDisposable OpenTypedEnvelope(Utf8JsonWriter writer, string typeDescriptor);
}
