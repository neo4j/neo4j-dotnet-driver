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
using System.Globalization;
using System.Linq;

namespace Neo4j.Driver.Internal.QueryApi;

// The single source of truth for one vector element type: its .NET type, the Typed-JSON wire
// `coordinatesType` name, the numeric format for its coordinate strings (null = default integer
// format), and how to parse coordinates back into a typed Vector.
internal abstract class VectorType
{
    public abstract Type ElementType { get; }

    public abstract string WireName { get; }

    public abstract string? NumberFormat { get; }

    public abstract Vector Build(string[] coordinates);

    public string Format(object value)
    {
        return ((IFormattable)value).ToString(NumberFormat, CultureInfo.InvariantCulture);
    }
}

internal sealed class VectorType<T>(string wireName, string? numberFormat) : VectorType
    where T : struct, IParsable<T>
{
    public override Type ElementType => typeof(T);

    public override string WireName => wireName;

    public override string? NumberFormat => numberFormat;

    public override Vector Build(string[] coordinates)
    {
        return Vector.Create(coordinates.Select(c => T.Parse(c, CultureInfo.InvariantCulture)).ToArray());
    }
}
