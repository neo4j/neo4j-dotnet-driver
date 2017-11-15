using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class ProcessBasedCommandRunner : ShellCommandRunner
    {
        private const int DefaultTimeOut = 2 * 60 * 1000; // 2 minutes

        private List<string> _stdOut;
        private StringBuilder _stdErr;
        private Process _process;

        private static Process CreateProcess(string command, string[] arguments, List<string> captureStdOut, StringBuilder captureStdErr)
        {
            var result = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = command,
                    Arguments = string.Join(" ", arguments),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            result.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    captureStdOut.Add(e.Data);
                }
            };
            result.ErrorDataReceived += (s, e) =>
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
                    Debug($"{process.ExitCode}");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Timed out to execute shell command `{GetProcessCommandLine(process)}` after {DefaultTimeOut}ms");
                }

                if (process.ExitCode != 0 || stdErr.Length > 0)
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

        public override string[] EndRunCommand()
        {
            try
            {
                // wait for a while to download, install and start database
                if (_process.WaitForExit(DefaultTimeOut))
                {
                    Debug($"{_process.ExitCode}");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Timed out to execute shell command `{GetProcessCommandLine(_process)}` after {DefaultTimeOut}ms");
                }

                if (_process.ExitCode != 0 || _stdErr.Length > 0)
                {
                    throw new InvalidOperationException(
                        $"Failed to execute `{GetProcessCommandLine(_process)}` due to error:{Environment.NewLine}{_stdErr}" +
                        $"{Environment.NewLine}output:{Environment.NewLine}{_stdOut.ToContentString($"{Environment.NewLine}")}");
                }
                
                return _stdOut.ToArray();
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
}
