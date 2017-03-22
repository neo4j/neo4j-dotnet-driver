// Copyright (c) 2002-2017 "Neo Technology,"
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
using System.IO;
using System.Linq;
using Neo4j.Driver.IntegrationTests.Internals;

namespace Neo4j.Driver.IntegrationTests
{
    public class BoltkitHelper
    {
        public const string TestRequireBoltkit = "Boltkit required to run test not accessible";
        public static readonly string BoltkitArgs = Environment.GetEnvironmentVariable("neoctrl.args") ?? "-e 3.1.2";
        public static readonly string TargetDir = new DirectoryInfo("../../../../Target").FullName;

        public static bool IsAvaliable()
        {
            try
            {
                WindowsPowershellRunner.RunCommand("neoctrl-cluster", "--help");
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static string ServerVersion()
        {
            // the last of the args is the version to installed
            var strings = BoltkitArgs.Split(null);
            return strings.Last();
        }
    }
}