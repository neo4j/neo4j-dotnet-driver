// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using TechTalk.SpecFlow;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    [Binding]
    public static class TckHooks
    {
        private static IDriver _driver;
        public static INeo4jInstaller Installer;
        public const string Uri = "bolt://localhost";
        public static IAuthToken AuthToken;

        public static ISession CreateSession()
        {
            var session = _driver.Session();
            ScenarioContext.Current.Set(session);
            return session;
        }

        [AfterScenario]
        public static void DisposeSession()
        {
            ISession session;
            ScenarioContext.Current.TryGetValue(out session);
            session?.Dispose();
        }

        [BeforeTestRun]
        public static void GlobalBeforeTestRun()
        {
            Installer = new ExternalPythonInstaller();
            Installer.DownloadNeo4j();
            try
            {
                Installer.InstallServer();
                Installer.StartServer();
            }
            catch
            {
                try
                {
                    GlobalAfterTestRun();
                }
                catch
                {
                    /*Do Nothing*/
                }
                throw;
            }
            AuthToken = AuthTokens.Basic("neo4j", "neo4j");
            CreateNewDriver();
        }

        [AfterTestRun]
        public static void GlobalAfterTestRun()
        {
            DisposeDriver();

            try
            {
                Installer.StopServer();
            }
            catch
            {
                // ignored
            }
            Installer.UninstallServer();
        }

        [BeforeFeature]
        public static void BeforeFeature()
        {
            Console.WriteLine("Feature: " + FeatureContext.Current.FeatureInfo.Title);
        }

        [AfterScenario]
        public static void AfterScenario()
        {
            if (ScenarioContext.Current.TestError != null)
            {
                Console.WriteLine($"\nScenario: {ScenarioContext.Current.ScenarioInfo.Title}");
                Console.WriteLine($"{ScenarioContext.Current.TestError}");
            }
        }

        private static void DisposeDriver()
        {
            _driver?.Dispose();
        }

        private static void CreateNewDriver()
        {
            _driver = GraphDatabase.Driver(Uri, AuthToken);
        }
    }
}
