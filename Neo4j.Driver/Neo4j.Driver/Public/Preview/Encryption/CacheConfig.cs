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

namespace Neo4j.Driver.Preview.Encryption;

/// <summary>
/// The bounds of one property encryption cache. This record is part of the Encryption Preview feature,
/// and is subject to change or removal.
/// </summary>
/// <param name="MaxSize">
/// The greatest number of entries the cache retains. Adding an entry beyond this evicts the least
/// recently used one.
/// </param>
/// <param name="Ttl">How long an entry may be used for before it is treated as absent.</param>
public record CacheConfig(int MaxSize, TimeSpan Ttl);
