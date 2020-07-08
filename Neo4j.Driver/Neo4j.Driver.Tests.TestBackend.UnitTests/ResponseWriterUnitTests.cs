using Moq;
using Neo4j.Driver.Tests.TestBackend;
using Xunit;
using FluentAssertions;
using System.IO;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend.UnitTests
{
    public class ResponseWriterUnitTests
    {
        [Theory]
        [InlineData(Protocol.Types.NewDriver)]
        [InlineData(Protocol.Types.NewSession)]
        [InlineData(Protocol.Types.AuthorizationToken)]
        [InlineData(Protocol.Types.SessionReadTransaction)]
        [InlineData(Protocol.Types.DriverClose)]
        [InlineData(Protocol.Types.SessionClose)]
        //[InlineData(Protocol.Types.ResultNext)]
        //[InlineData(Protocol.Types.SessionRun)]
        //[InlineData(Protocol.Types.TransactionRun)]   //TODO... need to reimplement these.
        public async Task ShouldWriteValidResponse(Protocol.Types objectType)
        {
            var moqStream = new Mock<Stream>();
            moqStream.Setup(x => x.CanWrite).Returns(true);
            var moqWriter = new Mock<Writer>(moqStream.Object);

            var responseWriter = new ResponseWriter(moqWriter.Object);
            ProtocolObjectFactory.ObjManager = new ProtocolObjectManager();
            IProtocolObject protocolObject = ProtocolObjectFactory.CreateObject(objectType);

            var resultString = await responseWriter.WriteResponseAsync(protocolObject);

            resultString.Should().Be("#response begin\n" + protocolObject.Respond() + "\n#response end");
        }
    }
}
