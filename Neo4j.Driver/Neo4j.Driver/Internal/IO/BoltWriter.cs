using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.V1;
using static Neo4j.Driver.Internal.IO.PackStream;

namespace Neo4j.Driver.Internal.IO
{
    internal class BoltWriter: IBoltWriter, IMessageRequestHandler
    {
        private static readonly Dictionary<string, object> EmptyDictionary = new Dictionary<string, object>();
        private static readonly byte[] MessageBoundary = new byte[0];

        private readonly IChunkWriter _chunkWriter;
        private readonly PackStreamWriter _packStreamWriter;
        private readonly MemoryStream _bufferStream;
        private readonly ILogger _logger;

        public BoltWriter(Stream stream)
            : this(stream, true)
        {
            
        }

        public BoltWriter(Stream stream, bool supportBytes)
            : this(stream, null, supportBytes)
        {

        }

        public BoltWriter(Stream stream, ILogger logger, bool supportBytes)
            : this(new ChunkWriter(stream, logger), logger, supportBytes)
        {

        }

        public BoltWriter(IChunkWriter chunkWriter, ILogger logger, bool supportBytes)
        {
            Throw.ArgumentNullException.IfNull(chunkWriter, nameof(chunkWriter));

            _logger = logger;
            _chunkWriter = chunkWriter;
            _bufferStream = new MemoryStream();
            _packStreamWriter = supportBytes ? new PackStreamWriter(_bufferStream) : new PackStreamWriterBytesIncompatible(_bufferStream);
        }

        public void Write(IRequestMessage message)
        {
            message.Dispatch(this);

            // write buffered message into chunk writer
            byte[] buffer = _bufferStream.GetBuffer();
            _chunkWriter.WriteChunk(buffer, 0, (int)_bufferStream.Length);
            _bufferStream.SetLength(0);

            // add message boundary
            _chunkWriter.WriteChunk(MessageBoundary, 0, MessageBoundary.Length);
        }

        public void Flush()
        {
            _chunkWriter.Flush();
        }

        public Task FlushAsync()
        {
            return _chunkWriter.FlushAsync();
        }

        public void HandleInitMessage(string clientNameAndVersion, IDictionary<string, object> authToken)
        {
            _packStreamWriter.WriteStructHeader(1, MsgInit);
            _packStreamWriter.Write(clientNameAndVersion);
            _packStreamWriter.Write(authToken ?? EmptyDictionary);
        }

        public void HandleRunMessage(string statement, IDictionary<string, object> parameters)
        {
            _packStreamWriter.WriteStructHeader(2, MsgRun);
            _packStreamWriter.Write(statement);
            _packStreamWriter.Write(parameters ?? EmptyDictionary);
        }

        public void HandlePullAllMessage()
        {
            _packStreamWriter.WriteStructHeader(0, MsgPullAll);
        }

        public void HandleDiscardAllMessage()
        {
            _packStreamWriter.WriteStructHeader(0, MsgDiscardAll);
        }

        public void HandleResetMessage()
        {
            _packStreamWriter.WriteStructHeader(0, MsgReset);
        }

        public void HandleAckFailureMessage()
        {
            _packStreamWriter.WriteStructHeader(0, MsgAckFailure);
        }
        
    }
}
