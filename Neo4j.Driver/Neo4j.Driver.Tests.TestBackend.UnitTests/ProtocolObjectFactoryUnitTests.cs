using FluentAssertions;
using Neo4j.Driver.Tests.TestBackend;
using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.UnitTests
{
    public class ProtocolObjectFactoryUnitTests
    {
        [Fact]
        public void ShouldCreateMultipleDriverObjectsNoThrow()
        {  
            const int count = 2;
            var objManager = new ProtocolObjectManager();

            for (int i = 0; i < count; i++)
            {
                ProtocolObjectFactory.ObjManager = objManager;
                var newObject = ProtocolObjectFactory.CreateObject(Protocol.Types.NewDriver);

                newObject.Should().BeOfType<NewDriver>();
                var newDriver = (NewDriver)newObject;
                newDriver.uniqueId.Should().Be(i.ToString());
            }

            objManager.ObjectCount.Should().Be(count);
        }

        [Fact]
        public void ShouldCreateMultipleSessionObjectsNoThrow()
        {   
            const int count = 2;
            var objManager = new ProtocolObjectManager();
            ProtocolObjectFactory.ObjManager = objManager;

            for (int i = 0; i < count; i++)
            {  
                var newObject = ProtocolObjectFactory.CreateObject(Protocol.Types.NewSession);

                newObject.Should().BeOfType<NewSession>();
                var newSession = (NewSession)newObject;
                newSession.uniqueId.Should().Be(i.ToString());
            }

            objManager.ObjectCount.Should().Be(count);
        }

        [Fact]
        public void ShouldCreateAuthorizationTokenNoThrow()
        {
            ProtocolObjectFactory.ObjManager = new ProtocolObjectManager();
            var newObject = ProtocolObjectFactory.CreateObject(Protocol.Types.AuthorizationToken);
            newObject.Should().BeOfType<AuthorizationToken>();
        }

        [Fact]
        public void ShouldCreateSessionRunNoThrow()
        {
            ProtocolObjectFactory.ObjManager = new ProtocolObjectManager();
            var newObject = ProtocolObjectFactory.CreateObject(Protocol.Types.SessionRun);
            newObject.Should().BeOfType<SessionRun>();
        }

        [Fact]
        public void ShouldRunQueryNoThrow()
        {
            //TODO: implement
        }
    }
}
