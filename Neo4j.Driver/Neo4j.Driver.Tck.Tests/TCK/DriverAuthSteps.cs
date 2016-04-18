// Copyright (c) 2002-2016 "Neo Technology,"
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Neo4j.Driver.Internal;
using TechTalk.SpecFlow;
using Xunit;
using Path = System.IO.Path;

namespace Neo4j.Driver.Tck.Tests.TCK
{
    [Binding]
    public class DriverAuthSteps : TckStepsBase
    {
        private static void RestartServerWithUpdatedSettings(IDictionary<string, string> keyValuePair)
        {
            try
            {
                Installer.StopServer();
                Installer.UpdateSettings(keyValuePair);
                Installer.StartServer();
            }
            catch
            {
                try { StopServer(); } catch { /*Do nothing*/ }
                throw;
            }
        }

        private static void StopServer()
        {
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

        private static string _authFilePath;

        [BeforeFeature("@auth")]
        public static void ChangeDefaultPasswordAndDriver()
        {
            DisposeDriver();
            _authFilePath = Path.Combine(Installer.Neo4jHome.FullName, "data/dbms/auth");
            if (File.Exists(_authFilePath))
            {
                File.Delete(_authFilePath);
            }
            RestartServerWithUpdatedSettings(new Dictionary<string, string>
            {
                {"dbms.security.auth_enabled", "true"}
            });
            using (var driver = GraphDatabase.Driver(Url, AuthTokens.Basic("neo4j", "neo4j"),
                Config.Builder.WithLogger(new DebugLogger {Level = LogLevel.Trace}).ToConfig()))
            {
                using (var session = driver.Session())
                {
                    var exception = Record.Exception(() => session.Run("CREATE () RETURN 2 as Number").ToList());
                    exception.Should().BeOfType<ClientException>();
                    exception.Message.Should().StartWith("The credentials you provided were valid");
                }
            }
            // update auth and run something
            using (var driver = GraphDatabase.Driver(
                Url,
                new AuthToken(new Dictionary<string, object>
                {
                    {"scheme", "basic"},
                    {"principal", "neo4j"},
                    {"credentials", "neo4j"},
                    {"new_credentials", "lala"}
                }),
                Config.Builder.WithLogger(new DebugLogger {Level = LogLevel.Trace}).ToConfig()))
            using (var session = driver.Session())
            {
                var resultCursor = session.Run("RETURN 1 as Number");
                resultCursor.Keys.Should().Contain("Number");
                resultCursor.Keys.Count.Should().Be(1);
            }
        }

        [AfterFeature("@auth")]
        public static void RestoreDefaultPasswordAndDriver()
        {
            File.Delete(_authFilePath);
            RestartServerWithUpdatedSettings(new Dictionary<string, string>
            {
                {"dbms.security.auth_enabled", "false"}
            });
            CreateNewDriver();
        }

        [Given(@"a driver is configured with auth enabled and correct password is provided")]
        public void GivenADriverIsConfiguredWithAuthEnabledAndCorrectPasswordIsProvided()
        {
            var driverWithCorrectPassword = GraphDatabase.Driver(Url, AuthTokens.Basic("neo4j", "lala"));
            ScenarioContext.Current.Set(driverWithCorrectPassword);
        }
        
        [Given(@"a driver is configured with auth enabled and the wrong password is provided")]
        public void GivenADriverIsConfiguredWithAuthEnabledAndTheWrongPasswordIsProvided()
        {
            var driverWithIncorrectPassword = GraphDatabase.Driver(Url, AuthTokens.Basic("neo4j", "toufu"));
            ScenarioContext.Current.Set(driverWithIncorrectPassword);
        }
        
        [Then(@"reading and writing to the database should be possible")]
        public void ThenReadingAndWritingToTheDatabaseShouldBePossible()
        {
            var driver = ScenarioContext.Current.Get<IDriver>();
            using (driver)
            using (var session = driver.Session())
            {
                var result = session.Run("CREATE () RETURN 2 as Number");
                result.Keys.Should().Contain("Number");
                result.Keys.Count.Should().Be(1);
                result.Single()["Number"].As<int>().Should().Be(2);
            }
        }

        [Then(@"reading and writing to the database should not be possible")]
        public void ThenReadingAndWritingToTheDatabaseShouldNotBePossible()
        {
            var driver = ScenarioContext.Current.Get<IDriver>();
            using (driver)
            using (var session = driver.Session())
            {
                var exception = Record.Exception(() => session.Run("CREATE () RETURN 2 as Number"));
                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().StartWith("The client is unauthorized due to authentication failure");
            }
        }
        
        [Then(@"a `Protocol Error` is raised")]
        public void ThenAProtocolErrorIsRaised()
        {
            // the check is done in ThenReadingAndWritingToTheDatabaseShouldNotBePossible
        }
    }
}
