﻿// Copyright (c) "Neo4j"
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Messaging
{
    internal class RunMessage : IRequestMessage
    {
        public RunMessage(string statement, IDictionary<string, object> statementParameters = null)
        {
            Statement = statement;
            StatementParameters = statementParameters;
        }

        public RunMessage(Statement statement)
        {
            Statement = statement.Text;
            StatementParameters = statement.Parameters;
        }

        public string Statement { get; }

        public IDictionary<string, object> StatementParameters { get; }

        public override string ToString()
        {
            return $"RUN `{Statement}` {StatementParameters.ToContentString()}";
        }
    }
}
