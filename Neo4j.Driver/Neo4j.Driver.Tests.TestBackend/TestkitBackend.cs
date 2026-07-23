// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

using System.Net;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace Neo4j.Driver.Tests.TestBackend;

public class TestkitBackend
{
    public static Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .UseServiceProviderFactory(new AutofacServiceProviderFactory(b => b.RegisterModule<BackendModule>()))
            .UseSerilog((_, logger) => logger.WriteTo.Console())
            .ConfigureAppConfiguration(config => config.AddInMemoryCollection(MapLaunchArgs(args)))
            .ConfigureWebHostDefaults(host => host
                .Configure(_ => { })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<BackendOptions>(context.Configuration.GetSection("Backend"));
                    services.AddSingleton<TestkitConnectionHandler>();
                })
                .UseKestrel((context, kestrel) =>
                {
                    var options = context.Configuration.GetSection("Backend").Get<BackendOptions>()!;
                    kestrel.Listen(
                        IPAddress.Parse(options.Address),
                        options.Port,
                        listen => listen.UseConnectionHandler<TestkitConnectionHandler>());
                }))
            .Build();
                  
        Console.WriteLine("Testkit backend starting up...");
        return host.RunAsync();
    }

    private static Dictionary<string, string?> MapLaunchArgs(string[] args)
    {
        // testkit launches the backend with positional args, which
        // override these three config values if present
        string[] positionalArgsOverrides = ["Backend:Address", "Backend:Port", "Backend:LogFile"];

        var overrides = new Dictionary<string, string?>();
        for(var i = 0; i < positionalArgsOverrides.Length; i++)
        {
            if (args.Length > i)
            {
                overrides[positionalArgsOverrides[i]] = args[i];
            }
        }

        return overrides;
    }
}
