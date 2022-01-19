﻿using System;
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
			
			FeaturesList.Add("Optimization:EagerTransactionBegin");

			FeaturesList.Add("Feature:Auth:Bearer");
			FeaturesList.Add("Feature:Auth:Custom");
			FeaturesList.Add("Feature:Auth:Kerberos");
			FeaturesList.Add("Feature:Impersonation");
			FeaturesList.Add("Feature:Bolt:3.0");
			FeaturesList.Add("Feature:Bolt:4.0");
			FeaturesList.Add("Feature:Bolt:4.1");
			FeaturesList.Add("Feature:Bolt:4.2");
			FeaturesList.Add("Feature:Bolt:4.3");
			FeaturesList.Add("Feature:Bolt:4.4");

			FeaturesList.Add("Feature:TLS:1.2");
			//FeaturesList.Add("Feature:TLS:1.3");

			//FeaturesList.Add("Feature:API:SSLSchemes");
			//FeaturesList.Add("Feature:API:Result.Single");
			//FeaturesList.Add("Feature:API:Result.Peek");
			//FeaturesList.Add("Feature:API:Result.List");

            FeaturesList.Add("Temporary:FullSummary");
			//FeaturesList.Add("Temporary:TransactionClose");
			//FeaturesList.Add("Temporary:DriverFetchSize");
		}
	}
}
