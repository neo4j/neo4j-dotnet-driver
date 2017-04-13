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
using System.Management.Automation;
using Neo4j.Driver.V1;
using static Neo4j.Driver.IntegrationTests.Internals.WindowsPowershellRunner;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    internal class BoltStubServer : IDisposable
    {
        public static readonly Config Config = new Config {EncryptionLevel = EncryptionLevel.None};
        private static readonly string ScriptSourcePath = new DirectoryInfo("../../Resources").FullName;

        private PowerShell _ps;
        private IAsyncResult _result;

        private BoltStubServer(string script, int port)
        {
            RunCommandAsyc("boltstub", port.ToString(), script);
        }

        public static BoltStubServer Start(string script, int port)
        {
            return new BoltStubServer(Source(script), port);
        }

        public void Dispose()
        {
            try
            {
                var results = _ps.EndInvoke(_result);
                if (!_ps.HadErrors)
                {
                    return;
                }
                foreach (var result in results)
                {
                    Debug(result.ToString());
                }
                throw new InvalidOperationException(CollectAsString(results));
            }
            finally
            {
                _ps?.Dispose();
            }
        }

        private void RunCommandAsyc(string command, params string[] arguments)
        {
            _ps = PowerShell.Create();
            _ps.AddCommand(command);
            foreach (var argument in arguments)
            {
                _ps.AddArgument(argument);
            }
            _result = _ps.BeginInvoke();
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
