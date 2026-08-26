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
using System.Collections.Generic;
using Neo4j.Driver.Tests.TestBackend.Protocol.Session;
using Neo4j.Driver.Tests.TestBackend.Types;
using Newtonsoft.Json.Linq;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Skip;

internal sealed class TryConstructPolicy : ITestSkipPolicy
{
    public TestDisposition Overall => TestDispositions.RunSubtests;

    public bool ShouldRunSubtest(IReadOnlyDictionary<string, JToken> parameters, out string skipReason)
    {
        foreach (var value in parameters.Values)
        {
            try
            {
                CypherToNative.Convert(JsonCypherParameterParser.ExtractParameterFromProperty(value as JObject));
            }
            catch (Exception ex)
            {
                skipReason = ex.Message;
                return false;
            }
        }

        skipReason = string.Empty;
        return true;
    }
}
