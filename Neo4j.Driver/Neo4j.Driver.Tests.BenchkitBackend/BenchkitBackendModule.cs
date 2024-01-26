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

using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Core.Resolving.Pipeline;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Neo4j.Driver.Tests.BenchkitBackend.Abstractions;
using Neo4j.Driver.Tests.BenchkitBackend.Implementations;

namespace Neo4j.Driver.Tests.BenchkitBackend;
using ILogger = Microsoft.Extensions.Logging.ILogger;

internal class BenchkitBackendModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<WorkloadStore>().As<IWorkloadStore>().SingleInstance();
        builder.RegisterType<WorkloadExecutor>().As<IWorkloadExecutor>().InstancePerDependency();
        builder.RegisterType<ActionContextAccessor>().As<IActionContextAccessor>().SingleInstance();
        builder.RegisterType<RecordConsumer>().As<IRecordConsumer>().SingleInstance();
        builder.RegisterType<DriverExecuteQueryMethod>().As<IWorkloadExecutionMethod>().Keyed<IWorkloadExecutionMethod>(Method.ExecuteQuery);//.InstancePerLifetimeScope();
    }

    /// <inheritdoc />
    protected override void AttachToComponentRegistration(IComponentRegistryBuilder componentRegistry, IComponentRegistration registration)
    {
        registration.PipelineBuilding += (_, pipeline) =>
        {
            // intercept injections of ILogger, and replace with a logger that has the correct category name
            pipeline.Use(PipelinePhase.ParameterSelection, MiddlewareInsertionMode.EndOfPhase, (context, next) =>
            {
                context.ChangeParameters(
                    context.Parameters.Union(
                        new[]
                        {
                            new ResolvedParameter(
                                (p, _) => p.ParameterType == typeof(ILogger),
                                (p, _) =>
                                {
                                    var type = typeof(ILogger<>).MakeGenericType(p.Member.DeclaringType!);
                                    var logger = context.Resolve(type);
                                    return logger;
                                })
                        }));

                next(context);
            });
        };
    }
}
