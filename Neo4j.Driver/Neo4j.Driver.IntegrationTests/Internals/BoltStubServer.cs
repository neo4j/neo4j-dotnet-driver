﻿// Copyright (c) 2002-2017 "Neo Technology,"
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
using System.Net.Sockets;
using System.Threading.Tasks;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Path = System.IO.Path;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    internal class BoltStubServer : IDisposable
    {
        public static readonly Config Config = new Config
        {
            EncryptionLevel = EncryptionLevel.None,
            Logger = new DebugLogger {Level = LogLevel.Debug}
        };
        private static readonly string ScriptSourcePath = new DirectoryInfo("../../Resources").FullName;

        private readonly IShellCommandRunner _commandRunner;
        private readonly int _port;
        private readonly TcpClient _testTcpClient = new TcpClient();

        private BoltStubServer(string script, int port)
        {
            _commandRunner = ShellCommandRunnerFactory.Create();
            _commandRunner.BeginRunCommand("boltstub", port.ToString(), script);
            _port = port;
            WaitForServer(_port);
        }

        public static BoltStubServer Start(string script, int port)
        {
            return new BoltStubServer(Source(script), port);
        }

        public void Dispose()
        {
            _testTcpClient.Dispose();
            _commandRunner.EndRunCommand();
            WaitForServer(_port, ServerStatus.Offline);
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
            Online, Offline
        }

        private void WaitForServer(int port, ServerStatus status = ServerStatus.Online)
        {
            var retryAttempts = 20;
            for (var i = 0; i < retryAttempts; i++)
            {
                ServerStatus currentStatus;
                try
                {
                    _testTcpClient.Connect("127.0.0.1", port);
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
            }
        }
    }
}
