using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal static class SupportedFeatures
	{
		public static List<string> FeaturesList { get; } = new List<string>();

		static SupportedFeatures()
		{
			FeaturesList.Add("AuthorizationExpiredTreatment");
			FeaturesList.Add("ConfHint:connection.recv_timeout_seconds");
		}
	}
}
