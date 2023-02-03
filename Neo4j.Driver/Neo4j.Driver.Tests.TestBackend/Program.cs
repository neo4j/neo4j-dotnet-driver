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
using System.Diagnostics;
using System.IO;
using System.Net;

namespace Neo4j.Driver.Tests.TestBackend;

public class Program
{
    private static IPAddress Address;
    private static uint Port;

    private static void Main(string[] args)
    {
        var consoleTraceListener = new TextWriterTraceListener(Console.Out);
        Trace.Listeners.Add(consoleTraceListener);

        try
        {
            ArgumentsValidation(args);

            using (var connection = new Connection(Address.ToString(), Port))
            {
                var controller = new Controller(connection);

                try
                {
                    controller.Process(true, e => { return true; }).Wait();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(
                        $"It looks like the ExceptionExtensions system has failed in an unexpected way. \n{ex}");
                }
                finally
                {
                    connection.StopServer();
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.Message);
            Trace.WriteLine($"Exception Details: \n {ex.StackTrace}");
        }
        finally
        {
            Trace.Flush();
            Trace.Listeners.Remove(consoleTraceListener);
            consoleTraceListener.Close();
            Trace.Close();
        }
    }

    private static void ArgumentsValidation(string[] args)
    {
        if (args.Length < 2)
        {
            throw new IOException(
                $"Incorrect number of arguments passed in. Expecting Address Port, but got {args.Length} arguments");
        }

        if (!uint.TryParse(args[1], out Port))
        {
            throw new IOException(
                $"Invalid port passed in parameter 2.  Should be unsigned integer but was: {args[1]}.");
        }

        if (!IPAddress.TryParse(args[0], out Address))
        {
            throw new IOException($"Invalid IPAddress passed in parameter 1. {args[0]}");
        }

        if (args.Length > 2)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(args[2]));
            Trace.WriteLine("Logging to file: " + args[2]);
        }

        Trace.WriteLine($"Starting TestBackend on {Address}:{Port}");
    }
}
