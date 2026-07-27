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

namespace Neo4j.Driver.TestKitBackend.Types;

internal interface IOptional<T>;

internal record Specified<T>(T Value) : IOptional<T>;

internal abstract class Missing;

file class MissingImpl<T>() : Missing, IOptional<T>
{
    public static MissingImpl<T> Instance { get; } = new();
}

internal static class Optional
{
    public static IOptional<T> Specified<T>(T value) => new Specified<T>(value);
    public static IOptional<T> Missing<T>() => MissingImpl<T>.Instance;

}
