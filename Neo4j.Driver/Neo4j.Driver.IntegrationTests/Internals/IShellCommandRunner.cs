// Copyright (c) 2002-2018 Neo4j Sweden AB [http://neo4j.com]
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

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public interface IShellCommandRunner
    {
        void Debug(string message);
        /// <summary>
        /// Run the given commands with the multiple command arguments in shell
        /// Return the shell output back
        /// </summary>
        string[] RunCommand(string command, params string[] arguments);
        void BeginRunCommand(string command, params string[] arguments);
        string[] EndRunCommand();
    }

    public abstract class ShellCommandRunner : IShellCommandRunner
    {
        public abstract string[] RunCommand(string command, params string[] arguments);
        public abstract void BeginRunCommand(string command, params string[] arguments);
        public abstract string[] EndRunCommand();

        public virtual void Debug(string message)
        {
            Console.WriteLine(message);
        }

        protected static string ShellCommandArgument(string command, string[] arguments)
        {
            return $"{command} {string.Join(" ", arguments)}";
        }
    }

    public class ShellCommandRunnerFactory
    {
        public static IShellCommandRunner Create()
        {
            return new ProcessBasedCommandRunner();
        }
    }
}
