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
using Autofac.Core.Resolving.Pipeline;
using Microsoft.Extensions.Logging;

namespace Neo4j.Driver.TestKitBackend.Infrastructure;

public class LoggerMiddleware : IResolveMiddleware
{
    public PipelinePhase Phase => PipelinePhase.ParameterSelection;

    public void Execute(ResolveRequestContext context, Action<ResolveRequestContext> next)
    {
        context.ChangeParameters(
            context.Parameters.Union(
            [
                new ResolvedParameter(
                    predicate: (p, _) => p.ParameterType == typeof(ILogger),
                    valueAccessor: (p, ctx) => ctx.Resolve<ILoggerFactory>().CreateLogger(p.Member.DeclaringType!))
            ]));

        next(context);
    }
}
