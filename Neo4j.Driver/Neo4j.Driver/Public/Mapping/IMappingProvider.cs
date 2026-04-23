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

namespace Neo4j.Driver.Mapping;

/// <summary>
/// Implement this interface to register one or more type mappings using the fluent
/// <see cref="IMappingBuilder{TObject}"/> API.
/// </summary>
/// <remarks>
/// <para>
/// A mapping provider is the recommended pattern for configuring multiple types at startup. Implement
/// <see cref="CreateMappers"/> and call <see cref="IMappingRegistry.RegisterMapping{T}"/> for each type you
/// want to customise. Then register the provider once at startup:
/// </para>
/// <code language="csharp">
/// public class MyMappingProvider : IMappingProvider
/// {
///     public void CreateMappers(IMappingRegistry registry)
///     {
///         registry.RegisterMapping&lt;Person&gt;(b => b
///             .UseDefaultMapping()
///             .Map(p => p.Labels, "person", MappingSource.NodeLabel));
///
///         registry.RegisterMapping&lt;Address&gt;(b => b
///             .Map(a => a.Street, "street")
///             .Map(a => a.City, "city"));
///     }
/// }
///
/// // At startup:
/// RecordObjectMapping.RegisterProvider&lt;MyMappingProvider&gt;();
/// </code>
/// </remarks>
public interface IMappingProvider
{
    /// <summary>
    /// Called once when the provider is registered. Use the supplied <paramref name="registry"/> to register
    /// mappings for all types this provider is responsible for.
    /// </summary>
    /// <param name="registry">The registry to register mappings with.</param>
    void CreateMappers(IMappingRegistry registry);
}
