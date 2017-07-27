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
    public class PackStreamWriterBytesIncompatibleTests: PackStreamWriterTests
    {

        [Fact]
        public void ShouldThrowWhenBytesIsSent()
        {
            var mocks = new Mocks();
            var writer = new PackStreamWriterBytesIncompatible(mocks.OutputStream);

            var ex = Record.Exception(() => writer.Write(new byte[10]));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

    }
}
