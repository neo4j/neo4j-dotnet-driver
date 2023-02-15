// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Neo4j.Driver.IntegrationTests.Internals;

public interface IShellCommandRunner
{
    void Debug(string message);

    /// <summary>Run the given commands with the multiple command arguments in shell Return the shell output back</summary>
    string[] RunCommand(string command, params string[] arguments);

    void BeginRunCommand(string command, params string[] arguments);
    void EndRunCommand();
}

public abstract class ShellCommandRunner : IShellCommandRunner
{
    public abstract string[] RunCommand(string command, params string[] arguments);
    public abstract void BeginRunCommand(string command, params string[] arguments);
    public abstract void EndRunCommand();

    public virtual void Debug(string message)
    {
        System.Diagnostics.Debug.WriteLine(message);
    }
}

public sealed class ShellCommandRunnerFactory
{
    public static IShellCommandRunner Create()
    {
        return new ProcessBasedCommandRunner();
    }
}
