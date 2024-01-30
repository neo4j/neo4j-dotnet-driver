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

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using Microsoft.OpenApi.Models;
using Neo4j.Driver.Tests.BenchkitBackend;
using Neo4j.Driver.Tests.BenchkitBackend.Configuration;
using Neo4j.Driver.Tests.BenchkitBackend.InfrastructureExtensions;
using Serilog;
using Serilog.Enrichers;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json")
    .OverrideFromBenchkitEnvironmentVars();

var benchkitBackendConfiguration = new BenchkitBackendConfiguration();
builder.Configuration.GetSection("BenchkitBackend").Bind(benchkitBackendConfiguration);

// override port we're listening on from configuration
builder.WebHost.UseKestrel(k => k.ListenAnyIP(benchkitBackendConfiguration.BackendPort));

// wire up autofac
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(b =>
    {
        b.RegisterInstance(benchkitBackendConfiguration).SingleInstance();
        b.RegisterModule<BenchkitBackendModule>();
        b.RegisterApiControllers(Assembly.GetExecutingAssembly());
    });

builder.Services
    .AddNeo4jDriver(benchkitBackendConfiguration)
    .AddEndpointsApiExplorer()
    .AddSwaggerGen(
        c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "BenchkitBackend", Version = "v1" });
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        })
    .AddControllers()
    .AddControllersAsServices()
    .AddJsonOptions(
        options =>
        {
            // setup JSON serialization so that camelCase is used for deserializing enums
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

// setup Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.With<ThreadIdEnricher>()
    .Enrich.With<ClassNameEnricher>()
    .WriteTo.Console(
        outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] <{SourceContext}:{ThreadId}> {Message:lj} {NewLine}{Exception}")
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

LogoWriter.WriteLogo();
var app = builder.Build();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "Benchkit Backend"); });

app.Run();
