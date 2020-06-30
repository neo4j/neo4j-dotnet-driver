using Moq;
using Neo4j.Driver.Tests.TestBackend;
using Xunit;
using FluentAssertions;
using System.IO;

namespace Neo4j.Driver.Tests.TestBackend.UnitTests
{
    public class ResponseWriterUnitTests
    {
        [Theory]
        [InlineData(Protocol.Types.NewDriver)]
        [InlineData(Protocol.Types.NewSession)]
        [InlineData(Protocol.Types.AuthorizationToken)]
        [InlineData(Protocol.Types.SessionRun)]
        [InlineData(Protocol.Types.TransactionRun)]
        [InlineData(Protocol.Types.Result)]
        [InlineData(Protocol.Types.SessionReadTransaction)]
        [InlineData(Protocol.Types.DriverClose)]
        [InlineData(Protocol.Types.SessionClose)]
        [InlineData(Protocol.Types.ResultNext)]
        public void ShouldWriteValidResponse(Protocol.Types objectType)
        {
            var moqStream = new Mock<Stream>();
            moqStream.Setup(x => x.CanWrite).Returns(true);
            var moqWriter = new Mock<Writer>(moqStream.Object);

            var responseWriter = new ResponseWriter(moqWriter.Object);
            var objFactory = new ProtocolObjectFactory(new ProtocolObjectManager());
            IProtocolObject protocolObject = objFactory.CreateObject(objectType);

            var resultString = responseWriter.WriteResponse(protocolObject);

            resultString.Should().Be("#response begin\n" + protocolObject.Response() + "\n#response end");
        }
    }
}
