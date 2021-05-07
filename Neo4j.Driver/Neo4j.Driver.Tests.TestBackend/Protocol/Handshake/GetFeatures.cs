using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class GetFeatures : IProtocolObject
	{
		public GetFeaturesType data { get; set; } = new GetFeaturesType();

		public class GetFeaturesType
		{

		}

		public override async Task Process()
		{
			await Task.CompletedTask;
		}

		public override string Respond()
		{
			return new ProtocolResponse("FeatureList", new
			{
				features = SupportedFeatures.FeaturesList
			}).Encode();
		}
	}
}
