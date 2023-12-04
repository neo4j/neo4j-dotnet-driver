﻿// Copyright (c) "Neo4j"
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

namespace Neo4j.Driver.Tests.TestBackend;

internal class CheckSessionAuthSupport : IProtocolObject
{
    public CheckSessionAuthSupportDto data { get; set; }

    public override string Respond()
    {
        var driver = (NewDriver)ObjManager.GetObject(data.driverId);
        var support = driver.Driver.SupportsSessionAuthAsync().GetAwaiter().GetResult();
        return new ProtocolResponse("SessionAuthSupport", new { id = uniqueId, available = support }).Encode();
    }

    public class CheckSessionAuthSupportDto
    {
        public string driverId { get; set; }
    }
}
