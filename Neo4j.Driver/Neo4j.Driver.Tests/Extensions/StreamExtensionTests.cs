using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using FluentAssertions;
using Moq;
using Xunit;
using Xunit.Abstractions;
using System.Threading;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Tests
{
	public class StreamExtensionTests
	{
		const int readTime = 200;

		[Fact]
		public async Task ShouldReadAndThrowOnTimeout()
		{
			const int timeout = 100;
			var moqMemoryStream = new Mock<Stream>();
			moqMemoryStream.Setup(x => x.ReadAsync(It.IsAny<Byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
						   .ReturnsAsync(100, TimeSpan.FromMilliseconds(readTime));

			var ex = await Record.ExceptionAsync(async () => await moqMemoryStream.Object.ReadWithTimeoutAsync(It.IsAny<byte[]>(), 
																												It.IsAny<int>(), 
																												It.IsAny<int>(), 
																												timeout));

			ex.Should().NotBeNull();
            ex.InnerException.Should().BeOfType<TaskCanceledException>();
			ex.Should()
				.BeOfType<ConnectionReadTimeoutException>()
				.Which.Message.Should()
				.Be($"Socket/Stream timed out after {timeout}ms, socket closed.");
		}

		[Theory]
		[InlineData(300)]
		[InlineData(0)]
		[InlineData(-1)]
		[InlineData(-1000)]
		public async Task ShouldReadSuccessfullyWithTimeout(int timeout)
		{
			var moqMemoryStream = new Mock<Stream>();
			moqMemoryStream.Setup(x => x.ReadAsync(It.IsAny<Byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
						   .ReturnsAsync(100, TimeSpan.FromMilliseconds(readTime));
			
			var ex = await Record.ExceptionAsync(async () => await moqMemoryStream.Object.ReadWithTimeoutAsync(It.IsAny<byte[]>(),
																										It.IsAny<int>(),
																										It.IsAny<int>(),
																										timeout));

			ex.Should().BeNull();
		}

		
	}
}
