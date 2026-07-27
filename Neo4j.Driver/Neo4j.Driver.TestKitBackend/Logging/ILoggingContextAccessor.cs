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

namespace Neo4j.Driver.TestKitBackend.Logging;

// Bridges the per-connection ILoggingContext to the process-wide Serilog enricher: the connection
// handler publishes its scope's context at the top of the connection's async flow, and anything
// logging within that flow (and only that flow) sees it via GetCurrent.
internal interface ILoggingContextAccessor
{
    void Publish(ILoggingContext context);
    ILoggingContext? GetCurrent();
}
