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

namespace Neo4j.Driver.Internal.IO.Utils;

internal class AsyncTestStream : Stream
{
    private readonly Exception _cause;
    private readonly Task<int> _result;
    private readonly MemoryStream _stream = new();

    private AsyncTestStream(Task<int> result)
    {
        _result = result;
        _cause = null;
    }

    private AsyncTestStream(Exception cause)
    {
        _cause = cause;
        _result = null;
    }

    public override bool CanRead => _stream.CanRead;
    public override bool CanSeek => _stream.CanSeek;
    public override bool CanWrite => _stream.CanWrite;
    public override long Length => _stream.Length;

    public override long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (_cause != null)
        {
            throw _cause;
        }

        return _result;
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (_cause != null)
        {
            throw _cause;
        }

        return _result;
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        if (_cause != null)
        {
            throw _cause;
        }

        return _result;
    }

    public override void Flush()
    {
        _stream.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _stream.SetLength(value);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _stream.Read(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _stream.Write(buffer, offset, count);
    }

    public static Stream CreateCancellingStream()
    {
        var tcs = new TaskCompletionSource<int>();
        tcs.SetCanceled();
        return new AsyncTestStream(tcs.Task);
    }

    public static Stream CreateFailingStream(Exception cause)
    {
        var tcs = new TaskCompletionSource<int>();
        tcs.SetException(cause);
        return new AsyncTestStream(tcs.Task);
    }

    public static Stream CreateSyncFailingStream(Exception cause)
    {
        return new AsyncTestStream(cause);
    }

    public static Stream CreateStream(Task<int> result)
    {
        return new AsyncTestStream(result);
    }
}
