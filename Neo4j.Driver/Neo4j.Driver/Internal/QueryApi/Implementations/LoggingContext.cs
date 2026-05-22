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

using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

internal class LoggingContext : ILoggingContext
{
    public LoggingContext(string key, object value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; }
    public object Value { get; }
}

internal static class LoggingContainerExtensions
{
    extension(IServiceRegistry registry)
    {
        public IServiceRegistry AddLoggingContext(string key, object value)
        {
            return registry.RegisterInstance<ILoggingContext>(new LoggingContext(key, value));
        }
    }
}
