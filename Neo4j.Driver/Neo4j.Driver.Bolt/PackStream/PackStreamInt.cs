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

namespace Neo4j.Driver.Bolt.PackStream;

/// <summary>
/// Inclusive bounds for signed integer widths. Used when choosing PackStream integer encodings;
/// see <see cref="Implementations.PackStreamWriter.WriteInteger"/> for tiny vs INT8-marker split within int8.
/// </summary>
internal static class PackStreamInt
{
    public const long TinyIntegerMin = -16;
    public const long Int8MarkerMax = -17;

    public const long MinInt8Value = -128;
    public const long MaxInt8Value = 127;

    public const long MinInt16Value = -32_768;
    public const long MaxInt16Value = 32_767;

    public const long MinInt32Value = int.MinValue;
    public const long MaxInt32Value = int.MaxValue;
}
