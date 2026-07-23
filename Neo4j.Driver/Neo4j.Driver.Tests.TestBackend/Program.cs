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
using Serilog;

namespace Neo4j.Driver.Tests.TestBackend;

public class Program
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

        return host.RunAsync();
    }

    // Testkit launches the backend as `<dll> <address> <port> [logfile]`; positional args
    // override appsettings.json.
    private static Dictionary<string, string?> MapLaunchArgs(string[] args)
    {
        var overrides = new Dictionary<string, string?>();
        if (args.Length > 0)
        {
            overrides["Backend:Address"] = args[0];
        }

        if (args.Length > 1)
        {
            overrides["Backend:Port"] = args[1];
        }

        if (args.Length > 2)
        {
            overrides["Backend:LogFile"] = args[2];
        }

        return overrides;
    }
}
