using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO
{
    public class PackStreamReaderBytesIncompatibleTests
    {

        [Fact]
        public void ShouldThrowWhenBytesIsSent()
        {
            var mockInput =
                IOExtensions.CreateMockStream("CC 01 01".ToByteArray());
            var reader = new PackStreamReaderBytesIncompatible(mockInput.Object);

            var ex = Record.Exception(() => reader.Read());

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

    }
}
