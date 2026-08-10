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

using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Connection;

[RegistrationLifetime(RegistrationLifetime.PerLifetimeScope)]
internal class ResponseWriter : IResponseWriter
{
    private readonly IConnectionOutput _output;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger _logger;

    public ResponseWriter(
        IConnectionOutput output,
        IMessageSerializer serializer,
        ILogger logger)
    {
        _output = output;
        _serializer = serializer;
        _logger = logger;
    }

    public async Task WriteAsync(IProtocolMessage message)
    {
        var json = _serializer.Serialize(message);
        _logger.LogDebug("Response: {Json}", json);
        await _output.WriteAsync($"#response begin\n{json}\n#response end\n");
        await _output.FlushAsync();
    }
}
