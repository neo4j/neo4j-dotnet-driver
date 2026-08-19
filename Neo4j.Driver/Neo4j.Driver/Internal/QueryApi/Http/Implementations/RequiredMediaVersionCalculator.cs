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

using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.Internal.DependencyInjection;

namespace Neo4j.Driver.Internal.QueryApi;

[AutoRegister]
internal class RequiredMediaVersionCalculator : IRequiredMediaVersionCalculator
{
    private readonly IQueryApiWriteCodecSelector _selector;

    public RequiredMediaVersionCalculator(IQueryApiWriteCodecSelector selector)
    {
        _selector = selector;
    }

    public QueryApiMediaVersion Calculate(IEnumerable<object?> values)
    {
        return values.Aggregate(QueryApiMediaVersion.V1_0, (current, value) =>
        {
            var required = RequiredVersionFor(value);
            return required > current ? required : current;
        });
    }

    private QueryApiMediaVersion RequiredVersionFor(object? value)
    {
        var codec = _selector.Select(value);
        if (codec is IQueryApiContainerCodec container)
        {
            return Calculate(container.GetChildValues(value!));
        }

        return codec.RequiredVersion;
    }
}
