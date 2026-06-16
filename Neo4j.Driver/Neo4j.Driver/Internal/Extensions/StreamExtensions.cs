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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal;

internal static class StreamExtensions
{
    /// <summary>
    /// The standard ReadAsync in .Net does not honor the CancellationToken even if supplied. This method wraps a call
    /// to ReadAsync in a task that monitors the token, and when detected calls the streams close method.
    /// </summary>
    /// <param name="stream">Stream instance that is being extended</param>
    /// <param name="buffer">Target buffer to write into</param>
    /// <param name="offset">Offset from which to begin writing data from the stream</param>
    /// <param name="count">The maximum number of bytes to read</param>
    /// <param name="timeoutMs">The timeout in milliseconds that the stream will close after if there is no activity.</param>
    /// <param name="cancellationToken">Token that can cancel the read independently of the timeout.</param>
    /// <returns>The number of bytes read</returns>
    public static async Task<int> ReadWithTimeoutAsync(
        this Stream stream,
        byte[] buffer,
        int offset,
        int count,
        int timeoutMs,
        CancellationToken cancellationToken = default)
    {
        if (timeoutMs <= 0)
        {
            // no timeout and high traffic code, so avoid allocation Cancellation token source.
            return await ReadWithoutTimeoutAsync(stream, buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }

        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));

        using var linked = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken)
            : null;

        var readToken = linked?.Token ?? timeoutSource.Token;

        try
        {
            // .netcore 3.0+ network streams support cancellation tokens.
            return await stream.ReadAsync(buffer.AsMemory(offset, count), readToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsCancellationException(ex))
        {
            // close the stream, the stream will be fully disposed later by SocketClient Dispose.
            stream.Close();

            if (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new ConnectionReadTimeoutException(
                    $"Socket/Stream timed out after {timeoutMs}ms, socket closed.",
                    ex);
            }

            // External cancellation — rethrow as-is so callers can distinguish it from a timeout.
            throw;
        }
    }

    private static Task<int> ReadWithoutTimeoutAsync(
        Stream stream,
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken = default)
    {
        // .netcore 3.0+ network streams support cancellation tokens.
        return stream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    private static bool IsCancellationException(Exception ex)
    {
        return ex is OperationCanceledException;
    }
}