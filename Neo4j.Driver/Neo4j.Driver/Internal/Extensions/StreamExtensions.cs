// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
    public static void Write(this Stream stream, byte[] bytes)
    {
        stream.Write(bytes, 0, bytes.Length);
    }

    public static int Read(this Stream stream, byte[] bytes)
    {
        var hasRead = 0;
        var offset = 0;
        var toRead = bytes.Length;

        do
        {
            hasRead = stream.Read(bytes, offset, toRead);
            offset += hasRead;
            toRead -= hasRead;
        } while (toRead > 0 && hasRead > 0);

        if (hasRead <= 0)
        {
            throw new IOException(
                $"Failed to read more from input stream. Expected {bytes.Length} bytes, received {offset}.");
        }

        return offset;
    }

    /// <summary>
    /// The standard ReadAsync in .Net does not honor the CancellationToken even if supplied. This method wraps a call
    /// to ReadAsync in a task that monitors the token, and when detected calls the streams close method.
    /// </summary>
    /// <param name="stream">Stream instance that is being extended</param>
    /// <param name="buffer">Target buffer to write into</param>
    /// <param name="offset">Offset from which to begin writing data from the stream</param>
    /// <param name="count">The maximum number of bytes to read</param>
    /// <param name="timeoutMs">The timeout in milliseconds that the stream will close after if there is no activity.</param>
    /// <returns>The number of bytes read</returns>
    public static async Task<int> ReadWithTimeoutAsync(
        this Stream stream,
        byte[] buffer,
        int offset,
        int count,
        int timeoutMs)
    {
        var timeout = timeoutMs <= 0
            ? TimeSpan.FromMilliseconds(-1)
            : TimeSpan.FromMilliseconds(timeoutMs);

        using var source = new CancellationTokenSource(timeout);

        try
        {
#if NET6_0_OR_GREATER
            var ctr = source.Token.Register(stream.Close);
            await using var _ = ctr.ConfigureAwait(false);
            return await stream.ReadAsync(buffer.AsMemory(offset, count), source.Token).ConfigureAwait(false);
#else
            using var _ = source.Token.Register(stream.Close);
            return await stream.ReadAsync(buffer, offset, count, source.Token).ConfigureAwait(false);
#endif
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or IOException &&
                                   source.IsCancellationRequested)
        {
            stream.Close();
            throw new ConnectionReadTimeoutException(
                $"Socket/Stream timed out after {timeoutMs}ms, socket closed.",
                ex);
        }
        catch
        {
            stream.Close();
            throw;
        }
    }
}
