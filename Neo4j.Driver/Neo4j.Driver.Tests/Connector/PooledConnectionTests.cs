using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Neo4j.Driver.Tests
{
	/* Exception types
	Exception
		Neo4jException
			ClientException						- Recoverable
				ValueTruncationException
				ValueOverflowException
				FatalDiscoveryException
				ResultConsumedException
			TransientException					- Recoverable
			DatabaseException
			ServiceUnavailableException
			SessionExpiredException
			ProtocolException
			SecurityException
				AuthenticationException
				AuthorizationException	
	*/
	public class PooledConnectionTests
	{
		[Theory]
		[InlineData(typeof(Neo4jException))]
		[InlineData(typeof(DatabaseException))]
		[InlineData(typeof(ServiceUnavailableException))]
		[InlineData(typeof(SessionExpiredException))]
		[InlineData(typeof(ProtocolException))]
		[InlineData(typeof(SecurityException))]
		[InlineData(typeof(AuthenticationException))]
		[InlineData(typeof(AuthorizationException))]
		public async Task ShouldHaveUnrecoverableErrorOnErrorAsync(Type exceptionType)
		{
			var connection = new Mock<IConnection>().Object;
			var releaseManager = new Mock<IConnectionReleaseManager>().Object;
			var pooledConnection = new PooledConnection(connection, releaseManager);
			var exception = (Exception)Activator.CreateInstance(exceptionType, "Testing exception");

			var resultingException = await Record.ExceptionAsync(async () => await pooledConnection.OnErrorAsync(exception));
			Assert.Equal(resultingException.GetType(), exceptionType);
			Assert.True(pooledConnection.HasUnrecoverableError);
		}

		[Theory]
		[InlineData(typeof(IOException))]
		[InlineData(typeof(SocketException))]		
		public async Task ShouldReturnConnectionErrorOnErrorAsync(Type exceptionType)
		{
			var connection = new Mock<IConnection>().Object;
			var releaseManager = new Mock<IConnectionReleaseManager>().Object;
			var pooledConnection = new PooledConnection(connection, releaseManager);
			var exception = (Exception)Activator.CreateInstance(exceptionType);

			var resultingException = await Record.ExceptionAsync(async () => await pooledConnection.OnErrorAsync(exception));
			Assert.Equal(resultingException.GetType(), typeof(ServiceUnavailableException));			
		}

		[Fact]
		public async Task ShouldCloseConnectionOnAuthorizationException()
		{
			var connection = new Mock<IConnection>();
			var releaseManager = new Mock<IConnectionReleaseManager>();
			var pooledConnection = new PooledConnection(connection.Object, releaseManager.Object);

			var resultException = await Record.ExceptionAsync(async () => await pooledConnection.OnErrorAsync(new AuthorizationException("Authorization error")));

			releaseManager.Verify(rm => rm.ReleaseAsync(pooledConnection), Times.Once());
		}

	}


}
