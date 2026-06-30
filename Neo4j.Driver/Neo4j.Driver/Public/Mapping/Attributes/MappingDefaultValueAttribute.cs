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

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Marks a property or constructor parameter as optional and specifies a fallback value to use when the
/// corresponding record field is absent. The mapper will not throw an exception if the field is missing.
/// This attribute has no effect when using custom-defined mappers.
/// </summary>
/// <remarks>
/// <para>
/// This attribute extends <see cref="MappingOptionalAttribute"/>, so applying it makes the member optional
/// implicitly — there is no need to also apply <see cref="MappingOptionalAttribute"/>.
/// </para>
/// <para>
/// The <paramref name="defaultValue"/> must be assignable to the property or parameter type at runtime.
/// </para>
/// </remarks>
/// <param name="defaultValue">The value to use when the record field is absent.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class MappingDefaultValueAttribute(object defaultValue) : MappingOptionalAttribute
{
    /// <summary>The default value to use if the property is not present in the record.</summary>
    public object DefaultValue => defaultValue;

    /// <inheritdoc />
    public override void Mutate(MappingBinding binding)
    {
        base.Mutate(binding);
        binding.DefaultValue = defaultValue;
    }
}
