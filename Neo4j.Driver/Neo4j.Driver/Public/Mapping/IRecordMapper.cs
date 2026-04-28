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
/// Implement this interface to provide a fully custom mapping from an <see cref="IRecord"/> to an object of
/// type <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the most flexible mapping option: your implementation has complete control over how the record
/// is read and how the object is constructed. Register your implementation with
/// <see cref="RecordObjectMapping.Register{T}(IRecordMapper{T})"/>.
/// </para>
/// <para>
/// For most scenarios, the default mapper (with optional attributes) or the fluent
/// <see cref="IMappingBuilder{TObject}"/> API via <see cref="IMappingProvider"/> are simpler alternatives.
/// Prefer this interface when the mapping logic is complex, stateful, or needs to share logic across multiple
/// record types.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of object to which records will be mapped.</typeparam>
public interface IRecordMapper<out T>
{
    /// <summary>Maps the given record to an object of type <typeparamref name="T"/>.</summary>
    /// <param name="record">The record to map.</param>
    /// <returns>The mapped object.</returns>
    T Map(IRecord record);
}
