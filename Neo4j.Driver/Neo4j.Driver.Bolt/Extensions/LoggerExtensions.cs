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

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Neo4j.Driver.Bolt.Extensions;

[SuppressMessage(
    "Usage", 
    "CA2254:Template should be a static expression", 
    Justification = "Logging utility method")]
public static class LoggerExtensions
{
    extension(ILogger logger)
    {
        public void LogIf(LogLevel logLevel, string template, Func<object[]> args)
        {
            if (logger.IsEnabled(logLevel))
            {
                logger.Log(logLevel, template, args());
            }
        }
    }
}
