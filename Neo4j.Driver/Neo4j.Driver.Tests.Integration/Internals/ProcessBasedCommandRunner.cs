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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.IntegrationTests.Internals;

public sealed class ProcessBasedCommandRunner : ShellCommandRunner, IDisposable
{
    private const int DefaultTimeOut = 4 * 60 * 1000; // 4 minutes
    private Process _process;
    private StringBuilder _stdErr;

    private List<string> _stdOut;

    public void Dispose()
    { 
        _process?.Dispose();
    }

    private static Process CreateProcess(
        string command,
        string[] arguments,
        List<string> captureStdOut,
        StringBuilder captureStdErr)
    {
        var result = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = string.Join(" ", arguments),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        result.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                captureStdOut.Add(e.Data);
            }
        };

        result.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                captureStdErr.AppendLine(e.Data);
            }
        };

        return result;
    }

    public override string[] RunCommand(string command, params string[] arguments)
    {
        var stdOut = new List<string>();
        var stdErr = new StringBuilder();

        using (var process = CreateProcess(command, arguments, stdOut, stdErr))
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (process.WaitForExit(DefaultTimeOut))
            {
                Debug("-----------");
                Debug($"Execution of command `{GetProcessCommandLine(process)}` exited with code {process.ExitCode}.");
                if (stdErr.Length > 0)
                {
                    Debug("The following output is generated:");
                    Debug(stdErr.ToString());
                }

                Debug("-----------");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Timed out to execute shell command `{GetProcessCommandLine(process)}` after {DefaultTimeOut}ms");
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Failed to execute `{GetProcessCommandLine(process)}` due to error:{Environment.NewLine}{stdErr}.");
            }
        }

        return stdOut.ToArray();
    }

    public override void BeginRunCommand(string command, params string[] arguments)
    {
        try
        {
            _stdOut = new List<string>();
            _stdErr = new StringBuilder();

            _process = CreateProcess(command, arguments, _stdOut, _stdErr);

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }
        catch (Exception)
        {
            CleanResources();

            throw;
        }
    }

    public override void EndRunCommand()
    {
        try
        {
            // wait for a while to download, install and start database
            if (_process.WaitForExit(DefaultTimeOut))
            {
                Debug("-----------");
                Debug(
                    $"Execution of command `{GetProcessCommandLine(_process)}` exited with code {_process.ExitCode}.");

                if (_stdErr.Length > 0)
                {
                    Debug("The following output is generated:");
                    Debug(_stdErr.ToString());
                }

                Debug("-----------");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Timed out to execute shell command `{GetProcessCommandLine(_process)}` after {DefaultTimeOut}ms");
            }

            if (_process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Failed to execute `{GetProcessCommandLine(_process)}` due to error (exit code: {_process.ExitCode}):" +
                    $"{Environment.NewLine}stderr:{Environment.NewLine}{_stdErr}" +
                    $"{Environment.NewLine}output:{Environment.NewLine}{_stdOut.ToContentString($"{Environment.NewLine}")}");
            }
        }
        finally
        {
            CleanResources();
        }
    }

    private void CleanResources()
    {
        _process?.Dispose();
        _process = null;
        _stdOut = null;
        _stdErr = null;
    }

    private static string GetProcessCommandLine(Process process)
    {
        return $"{process.StartInfo.FileName} {process.StartInfo.Arguments}";
    }
}
