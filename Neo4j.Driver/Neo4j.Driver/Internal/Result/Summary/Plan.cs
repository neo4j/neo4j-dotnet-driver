﻿// Copyright (c) "Neo4j"
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

using System.Collections.Generic;

namespace Neo4j.Driver.Internal.Result;

internal class Plan : IPlan
{
    public Plan(
        string operationType,
        IDictionary<string, object> args,
        IList<string> identifiers,
        IList<IPlan> childPlans)
    {
        OperatorType = operationType;
        Arguments = args;
        Identifiers = identifiers;
        Children = childPlans;
    }

    public string OperatorType { get; }
    public IDictionary<string, object> Arguments { get; }
    public IList<string> Identifiers { get; }
    public IList<IPlan> Children { get; }

    public override string ToString()
    {
        return $"{GetType().Name}{{{nameof(OperatorType)}={OperatorType}, " +
            $"{nameof(Arguments)}={Arguments.ToContentString()}, " +
            $"{nameof(Identifiers)}={Identifiers.ToContentString()}, " +
            $"{nameof(Children)}={Children.ToContentString()}}}";
    }
}
