// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal
{
    internal static class StreamExtensions
    {
        public static void Write(this Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static int Read(this Stream stream, byte[] bytes)
        {
            int hasRead = 0, offset = 0, toRead = bytes.Length;
            do
            {
                hasRead = stream.Read(bytes, offset, toRead);
                offset += hasRead;
                toRead -= hasRead;
            } while (toRead > 0 && hasRead > 0);

            if (hasRead <= 0)
            {
                throw new IOException($"Failed to read more from input stream. Expected {bytes.Length} bytes, received {offset}.");
            }
            return offset;
        }

		/// <summary>
		/// The standard ReadAsync in .Net does not honour the CancellationToken even if supplied. This method wraps a call to ReadAsync in a task that
		/// monitors the token, and when detected calls the streams close method.
		/// </summary>
		/// <param name="stream">Stream instance that is being extended</param>
		/// <param name="buffer">Target buffer to write into</param>
		/// <param name="offset">Offset from which to begin writing data from the stream</param>
		/// <param name="count">The maximum number of bytes to read</param>
		/// <param name="timeoutMS">The timeout in milliseconds that the stream will close after if there is no activity. </param>
		/// <returns>The number of bytes read</returns>
		public static async Task<int> ReadWithTimeoutAsync(this Stream stream, byte[] buffer,int offset, int count, int timeoutMS)
		{
			timeoutMS = (timeoutMS <= 0) ? -1 : timeoutMS;
			var cancellationDelayTask = Task.Delay(timeoutMS);
			int result;

			try
			{
				// Stream.ReadAsync doesn't honor cancellation token. It only checks it at the beginning. The actual
				// operation is not guarded. As a result if remote server never responds and connection never closed
				// it will lead to this operation hanging forever.
				Task<int> readBytesTask = stream.ReadAsync(buffer, offset, count);

				await Task.WhenAny(readBytesTask, cancellationDelayTask).ConfigureAwait(false);

				result = readBytesTask.Result;
			}
			catch(Exception ex)
			{
				if (ex.InnerException is not null)  //We want to throw the inner exception if anything goes wrong with the stream. If we don't
					throw ex.InnerException;        //then the Task.WhenAny exception will be propogated
				else                                
					throw;
			}
						
			// Check whether cancellation task is cancelled (or completed).
			if (cancellationDelayTask.IsCanceled || cancellationDelayTask.IsCompleted)
			{
				stream.Close();
				throw new ServiceUnavailableException($"Socket/Stream timed out afer {timeoutMS}ms, socket closed.");
			}

			// Means that main task completed. We use Result directly.
			// If the main task failed the following line will throw an exception and we'll catch it above.
			return result;
			
			
		}
	}
}
