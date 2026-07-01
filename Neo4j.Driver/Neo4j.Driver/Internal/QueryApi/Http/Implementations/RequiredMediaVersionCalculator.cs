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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class RequiredMediaVersionCalculator : IRequiredMediaVersionCalculator
{
    private readonly IEnumerable<IQueryApiTypeCodec> _codecs;

    public RequiredMediaVersionCalculator(IEnumerable<IQueryApiTypeCodec> codecs)
    {
        _codecs = codecs;
    }

    public QueryApiMediaVersion Calculate(IEnumerable<object?> values)
    {
        var version = QueryApiMediaVersion.V1_0;
        foreach (var value in values)
        {
            var required = RequiredVersionFor(value);
            if (required > version)
            {
                version = required;
            }
        }

        return version;
    }

    private QueryApiMediaVersion RequiredVersionFor(object? value)
    {
        if (value is IDictionary<string, object?> map)
        {
            return Calculate(map.Values);
        }

        if (value is IEnumerable enumerable and not string and not IDictionary and not byte[])
        {
            return Calculate(enumerable.Cast<object?>());
        }

        var codec = _codecs.FirstOrDefault(c => c.CanWrite(value));
        return codec?.RequiredVersion ?? QueryApiMediaVersion.V1_0;
    }
}
