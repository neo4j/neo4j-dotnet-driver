using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class UnixShellCommandRunner : ShellCommandRunner
    {
        private string _commandArgument;
        private List<string> _output;
        private StringBuilder _error;
        private AutoResetEvent _outputWaitHandle;
        private AutoResetEvent _errorWaitHandle;
        private Process _process;

        public override string[] RunCommand(string command, params string[] arguments)
        {
            var input = ShellCommandArgument(command, arguments);
            var timeout = 2 * 60 * 1000; // 2mins
            
            var output = new List<string>();
            var error = new StringBuilder();

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = string.Join(" ", arguments),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var outputWaitHandle = new AutoResetEvent(false))
            using (var errorWaitHandle = new AutoResetEvent(false))
            {
                try
                {
                    using (var process = new Process {StartInfo = startInfo})
                    {
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            {
                                Debug(e.Data);
                                output.Add(e.Data);
                            }
                        };
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                                errorWaitHandle.Set();
                            else
                                error.AppendLine(e.Data);
                        };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        // wait for a while to download, install and start database
                        if (process.WaitForExit(timeout))
                        {
                            Debug($"{process.ExitCode}");
                        }
                        else
                        {
                           throw new InvalidOperationException(
                                $"Timed out to execute shell command `{input}` after {timeout}ms");
                        }
                    }
                } // try
                finally
                {
                    outputWaitHandle.WaitOne(timeout);
                    errorWaitHandle.WaitOne(timeout);
                }
            }

            if (error.ToString().Length > 0)
            {
                throw new InvalidOperationException(
                    $"Failed to execute `{input}` due to error:{Environment.NewLine}{error}.");
            }

            return output.ToArray();
        }

        public override void BeginRunCommand(string command, params string[] arguments)
        {
            _commandArgument = ShellCommandArgument(command, arguments);
            
            _output = new List<string>();
            _error = new StringBuilder();

            _outputWaitHandle = new AutoResetEvent(false);
            _errorWaitHandle = new AutoResetEvent(false);

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = string.Join(" ", arguments),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                _process = new Process {StartInfo = startInfo};
                _process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        _outputWaitHandle.Set();
                    }
                    else
                    {
                        Debug(e.Data);
                        _output.Add(e.Data);
                    }
                };
                _process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                        _errorWaitHandle.Set();
                    else
                        _error.AppendLine(e.Data);
                };

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
                var timeout = 2 * 60 * 1000; // 2mins
                try
                {
                    // wait for a while to download, install and start database
                    if (_process.WaitForExit(timeout))
                    {
                        Debug($"{_process.ExitCode}");
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Timed out to execute shell command `{_commandArgument}` after {timeout}ms");
                    }
                }
                finally
                {
                    _outputWaitHandle.WaitOne(timeout);
                    _errorWaitHandle.WaitOne(timeout);
                }

                if (_error.ToString().Length > 0)
                {
                    throw new InvalidOperationException(
                        $"Failed to execute `{_commandArgument}` due to error:{Environment.NewLine}{_error}.");
                }
                
                return _output.ToArray();
            }
            finally
            {
                CleanResources();
            }
        }

        private void CleanResources()
        {
            _process?.Dispose();
            _errorWaitHandle?.Dispose();
            _outputWaitHandle?.Dispose();
            _commandArgument = null;
            _output = null;
            _error = null;
            _process = null;
            _errorWaitHandle = null;
            _outputWaitHandle = null;
        }
    }
}