﻿// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class StartTest : ProtocolObject
{
    public StartTestType data { get; set; } = new();

    [JsonIgnore] private string _reason;

    public override Task ProcessAsync()
    {
        if (TestBlackList.FindTest(data.testName, out var reason))
            _reason = reason;
        return Task.CompletedTask;
    }

    public override Task ReactiveProcessAsync()
    {
        if (TestBlackList.RxFindTest(data.testName, out var reason))
            _reason = reason;
        return Task.CompletedTask;
    }

    public override string Respond()
    {
        return _reason != null
            ? new ProtocolResponse("SkipTest", new { reason = _reason }).Encode()
            : new ProtocolResponse("RunTest").Encode();
    }

    public class StartTestType
    {
        public string testName { get; set; }
    }
}