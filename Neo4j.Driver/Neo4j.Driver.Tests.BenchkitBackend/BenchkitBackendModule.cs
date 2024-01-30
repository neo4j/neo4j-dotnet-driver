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
using Neo4j.Driver.Tests.BenchkitBackend.Types;

namespace Neo4j.Driver.Tests.BenchkitBackend;

using ILogger = Microsoft.Extensions.Logging.ILogger;

internal class BenchkitBackendModule : Module
{
    // register the services used by the controllers
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<WorkloadStore>().As<IWorkloadStore>().SingleInstance();
        builder.RegisterType<ActionContextAccessor>().As<IActionContextAccessor>().SingleInstance();

        builder.RegisterType<WorkloadExecutorSelector>().As<IWorkloadExecutorSelector>().InstancePerDependency();
        builder.RegisterType<RecordConsumer>().As<IRecordConsumer>().InstancePerDependency();

        builder.RegisterType<ExecuteQueryWorkloadExecutor>()
            .As<IWorkloadExecutor>()
            .Keyed<IWorkloadExecutor>(Method.ExecuteQuery)
            .InstancePerDependency();

        builder.RegisterType<SessionRunWorkloadExecutor>()
            .As<IWorkloadExecutor>()
            .Keyed<IWorkloadExecutor>(Method.SessionRun)
            .InstancePerDependency();

        builder.RegisterType<ExecuteReadWriteWorkloadExecutor>()
            .As<IWorkloadExecutor>()
            .Keyed<IWorkloadExecutor>(Method.ExecuteRead)
            .Keyed<IWorkloadExecutor>(Method.ExecuteWrite)
            .InstancePerDependency();
    }

    // allow all classes to just take a dependency on ILogger, and receive a logger with the correct category name
    protected override void AttachToComponentRegistration(
        IComponentRegistryBuilder componentRegistry,
        IComponentRegistration registration)
    {
        registration.PipelineBuilding += (_, pipeline) =>
        {
            pipeline.Use(
                PipelinePhase.ParameterSelection,
                MiddlewareInsertionMode.EndOfPhase,
                (context, next) =>
                {
                    context.ChangeParameters(
                        context.Parameters.Union(
                            new[]
                            {
                                new ResolvedParameter(
                                    (p, _) => p.ParameterType == typeof(ILogger),
                                    (p, _) =>
                                    {
                                        // replace ILogger with ILogger<T> where T is the type asking for the logger
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
