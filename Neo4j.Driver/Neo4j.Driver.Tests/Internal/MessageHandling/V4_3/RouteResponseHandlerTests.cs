using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Moq;
using Xunit;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Util;
using Neo4j.Driver.Internal.Result;


namespace Neo4j.Driver.Internal.MessageHandling.V4_3
{
	public class RouteResponseHandlerTests
	{
		[Fact]
		public void ShouldGetRouteInformationOnSuccess()
		{	
			var responseHandler = new RouteResponseHandler();
			var contexts = new Dictionary<string, object>() { { "Key1", "Value1" }, { "Key2", "Value2" }, { "Key3", "Value3" } };
			var	servers = new Dictionary<string, object>() { { "servers", contexts } } ;
			var metadata = new Dictionary<string, object>() { { "rt", servers } };
																		
			responseHandler.OnSuccess(metadata);

			responseHandler.RoutingInformation.Should().BeSameAs(servers);
		}
	}
}
