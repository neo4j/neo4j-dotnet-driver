// Copyright (c) "Neo4j"
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
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

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
        private readonly TcpClient _testTcpClient = new TcpClient();

        private BoltStubServer(string script, int port)
        {
            _commandRunner = ShellCommandRunnerFactory.Create();
            _commandRunner.BeginRunCommand("boltstub", "-v", port.ToString(), script);

            WaitForServer(port);
        }

        public static BoltStubServer Start(string script, int port)
        {
            return new BoltStubServer(Source(script), port);
        }

        public void Dispose()
        {
            try
            {
                Disconnect(_testTcpClient);
            }
            catch (Exception)
            {
                // ignored
            }

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

        private enum ServerStatus
        {
            Online,
            Offline
        }

        private void WaitForServer(int port, ServerStatus status = ServerStatus.Online)
        {
            var waitingTimeInSeconds = 15;
            var waitingTime = TimeSpan.FromSeconds(waitingTimeInSeconds).TotalMilliseconds;
            var stopwatch = Stopwatch.StartNew();
            do
            {
                ServerStatus currentStatus;
                try
                {
                    Connect(_testTcpClient, port);
                    if (_testTcpClient.Connected)
                    {
                        currentStatus = ServerStatus.Online;
                    }
                    else
                    {
                        currentStatus = ServerStatus.Offline;
                    }
                }
                catch (Exception)
                {
                    currentStatus = ServerStatus.Offline;
                }

                if (currentStatus == status)
                {
                    return;
                }

                // otherwise wait and retry
                Task.Delay(300).Wait();
            } while (stopwatch.ElapsedMilliseconds <= waitingTime);

            throw new InvalidOperationException(
                $"Waited for {waitingTimeInSeconds}s for stub server to be in {status} status, but failed.");
        }

        private void Disconnect(TcpClient testTcpClient)
        {
#if NET452
            testTcpClient.Close();
#else
            testTcpClient.Dispose();
#endif
        }

        private void Connect(TcpClient testTcpClient, int port)
        {
#if NET452
            testTcpClient.Connect("127.0.0.1", port);
#else
            Task.Run(() => testTcpClient.ConnectAsync("127.0.0.1", port)).Wait();
#endif
        }
    }
}