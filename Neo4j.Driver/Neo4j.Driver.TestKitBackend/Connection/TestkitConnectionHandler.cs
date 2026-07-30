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

using System.Text;
using Autofac;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Logging;

namespace Neo4j.Driver.TestKitBackend.Connection;

internal class TestkitConnectionHandler : ConnectionHandler
{
    private readonly ILifetimeScope _rootScope;
    private readonly IConnectionInputFactory _inputFactory;
    private readonly IConnectionOutputFactory _outputFactory;
    private readonly IConnectionIdProvider _connectionIdProvider;
    private readonly ILoggingContextAccessor _loggingContextAccessor;
    private readonly ILogger _logger;

    public TestkitConnectionHandler(
        ILifetimeScope rootScope,
        IConnectionInputFactory inputFactory,
        IConnectionOutputFactory outputFactory,
        IConnectionIdProvider connectionIdProvider,
        ILoggingContextAccessor loggingContextAccessor,
        ILogger logger)
    {
        _rootScope = rootScope;
        _inputFactory = inputFactory;
        _outputFactory = outputFactory;
        _connectionIdProvider = connectionIdProvider;
        _loggingContextAccessor = loggingContextAccessor;
        _logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        var connectionId = _connectionIdProvider.GetConnectionId();
        connection.ConnectionId = connectionId;

        // leaveOpen: we own these reader/writer wrappers, but Kestrel owns the underlying transport
        // pipe, so disposing them must not complete the pipe.
        var reader = new StreamReader(connection.Transport.Input.AsStream(leaveOpen: true), Encoding.UTF8);
        var writer = new StreamWriter(connection.Transport.Output.AsStream(leaveOpen: true), new UTF8Encoding(false))
        {
            NewLine = "\n"
        };

        var input = _inputFactory.Create(reader);
        var output = _outputFactory.Create(writer);

        // DI scope per connection == per test; the transport-bound input/output live only here.
        await using var scope = _rootScope.BeginLifetimeScope(builder =>
        {
            builder.RegisterInstance(input).As<IConnectionInput>();
            builder.RegisterInstance(output).As<IConnectionOutput>();
        });

        var loggingContext = scope.Resolve<ILoggingContext>();
        _loggingContextAccessor.Publish(loggingContext);
        loggingContext.Set("conn", connectionId);
        _logger.LogDebug("New connection {ConnectionId}", connectionId);

        try
        {
            await scope.Resolve<IMessageLoop>().RunAsync(connectionId);
        }
        finally
        {
            reader.Dispose();
            await writer.DisposeAsync();
        }
    }
}
