// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class ResolverResolutionCompleted : IProtocolObject
{
    public ResolverResolutionCompletedType data { get; set; } = new();

    [JsonIgnore] public ListAddressResolver Resolver { get; private set; }

    public override async Task Process()
    {
        await Task.CompletedTask;
    }

    public class ResolverResolutionCompletedType
    {
        public string requestId { get; set; }
        public List<string> addresses { get; set; } = new();
    }
}
