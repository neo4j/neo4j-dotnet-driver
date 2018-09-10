// Copyright (c) 2002-2018 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
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
using System.Reflection;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    internal class BoltStubServer : IDisposable
    {

        private static readonly string ScriptSourcePath;

        static BoltStubServer()
        {
            Uri assemblyUri = new Uri(typeof(BoltStubServer).GetTypeInfo().Assembly.CodeBase);
            string assemblyDirectory = new FileInfo(assemblyUri.AbsolutePath).Directory.FullName;
            
            ScriptSourcePath = Path.Combine(assemblyDirectory, "Resources");
        }
        
        private readonly IShellCommandRunner _commandRunner;

        private BoltStubServer(string script, int port)
        {
            _commandRunner = ShellCommandRunnerFactory.Create();
            _commandRunner.BeginRunCommand("boltstub", port.ToString(), script);
        }

        public static BoltStubServer Start(string script, int port)
        {
            return new BoltStubServer(Source(script), port);
        }

        public void Dispose()
        {
            _commandRunner.EndRunCommand();
        }

        private static string Source(string script)
        {
            var scriptFilePath = Path.Combine(ScriptSourcePath, $"{script}.script");
            if (!File.Exists(scriptFilePath))
            {
                throw new ArgumentException($"Cannot locate script file `{scriptFilePath}`", scriptFilePath);
            }
            return scriptFilePath;
        }
    }
}
