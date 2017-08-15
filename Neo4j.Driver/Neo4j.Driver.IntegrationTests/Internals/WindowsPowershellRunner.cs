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
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class WindowsPowershellRunner : ShellCommandRunner
    {
        private string _commandArgument;
        private PowerShell _ps;
        private IAsyncResult _beginResult;

        public override string[] RunCommand(string command, params string[] arguments)
        {
            using (var powershell = PowerShell.Create())
            {
                powershell.AddCommand(command);
                foreach (var argument in arguments)
                {
                    powershell.AddArgument(argument);
                }
                var results = powershell.Invoke();

                foreach (var result in results)
                {
                    Debug(result.ToString());
                }
                if (powershell.HadErrors)
                {
                    var errorMessage = CollectAsString(powershell.Streams.Error);
                    throw new Neo4jException("Integration", errorMessage);
                }
                return CollectionAsStringArray(results);
            }
        }

        public override void BeginRunCommand(string command, params string[] arguments)
        {
            try
            {
                _commandArgument = ShellCommandArgument(command, arguments);
                _ps = PowerShell.Create();
                _ps.AddCommand(command);
                foreach (var argument in arguments)
                {
                    _ps.AddArgument(argument);
                }
                _beginResult = _ps.BeginInvoke();
            }
            catch(Exception)
            {
                CleanResources();
                throw;
            }
        }

        public override string[] EndRunCommand()
        {
            try
            {
                var endResult = _ps.EndInvoke(_beginResult);

                foreach (var result in endResult)
                {
                    Debug(result.ToString());
                }
                if (_ps.HadErrors)
                {
                    var errorMessage = CollectAsString(_ps.Streams.Error);
                    throw new InvalidOperationException(
                        $"Failed to execute `{_commandArgument}` due to error:{Environment.NewLine}{errorMessage}");
                }
                return CollectionAsStringArray(endResult);
            }
            finally
            {
                CleanResources();
            }
        }

        private void CleanResources()
        {
            _ps?.Dispose();
            _ps = null;
            _commandArgument = null;
            _beginResult = null;

        }

        private static string[] CollectionAsStringArray<T>(IEnumerable<T> lines)
        {
            return lines.Select(error => error.ToString()).ToArray();
        }

        private static string CollectAsString<T>(IEnumerable<T> lines)
        {
            return string.Join(Environment.NewLine, CollectionAsStringArray(lines));
        }
    }
}
