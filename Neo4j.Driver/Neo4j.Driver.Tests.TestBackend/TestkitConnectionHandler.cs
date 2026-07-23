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

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Neo4j.Driver.Tests.TestBackend;

public class TestkitConnectionHandler : ConnectionHandler
{
    private readonly ILogger<TestkitConnectionHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TestkitConnectionHandler(IServiceScopeFactory scopeFactory, ILogger<TestkitConnectionHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        _logger.LogInformation("Testkit connected: {ConnectionId}", connection.ConnectionId);
        await using var scope = _scopeFactory.CreateAsyncScope();
        try
        {
            // Stub: consume and discard input until the connection closes.
            // Framing + dispatch replace this in the next milestones.
            while (true)
            {
                var result = await connection.Transport.Input.ReadAsync();
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        finally
        {
            _logger.LogInformation("Testkit disconnected: {ConnectionId}", connection.ConnectionId);
        }
    }
}
