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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;

namespace Neo4j.Driver.Internal.Routing;

internal class ClusterConnection : DelegatedConnection
{
    private readonly IErrorHandler _errorHandler;
    private readonly Uri _uri;

    public ClusterConnection(
        IConnection connection,
        Uri uri,
        IErrorHandler errorHandler)
        : base(connection)
    {
        _uri = uri;
        _errorHandler = errorHandler;
    }

    internal override async Task OnErrorAsync(Exception error)
    {
        await base.OnErrorAsync(error).ConfigureAwait(false);
        if (error is ServiceUnavailableException)
        {
            await _errorHandler.OnConnectionErrorAsync(_uri, Database, error).ConfigureAwait(false);
            throw new SessionExpiredException(
                $"Server at {_uri} is no longer available due to error: {error.Message}.",
                error);
        }

        if (error is ConnectionReadTimeoutException)
        {
            await _errorHandler.OnConnectionErrorAsync(_uri, Database, error).ConfigureAwait(false);
        }
        else if (error.IsDatabaseUnavailableError())
        {
            await _errorHandler.OnConnectionErrorAsync(_uri, Database, error).ConfigureAwait(false);
        }
        else
        {
            HandleClusterError(error);
        }

        throw error;
    }

    private void HandleClusterError(Exception error)
    {
        if (!error.IsClusterError())
        {
            return;
        }

        switch (Mode)
        {
            case AccessMode.Read:
                // The user was trying to run a write in a read session
                // So inform the user and let him try with a proper session mode
                throw new ClientException("Write queries cannot be performed in READ access mode.");

            case AccessMode.Write:
                // The lead is no longer a leader, a.k.a. the write server no longer accepts writes
                // However the server is still available for possible reads.
                // Therefore we just remove it from ClusterView but keep it in connection pool.
                _errorHandler.OnWriteError(_uri, Database);
                throw new SessionExpiredException($"Server at {_uri} no longer accepts writes");

            default:
                throw new ArgumentOutOfRangeException($"Unsupported mode type {Mode}");
        }
    }
}
