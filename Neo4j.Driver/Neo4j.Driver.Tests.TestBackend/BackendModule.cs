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
using Neo4j.Driver.Tests.TestBackend.Protocol;

namespace Neo4j.Driver.Tests.TestBackend;

public class BackendModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var singletons = new List<object> {
            MessageRegistry.FromAssembly(typeof(IProtocolMessage).Assembly)
        };

        builder
            .RegisterAssemblyTypes(typeof(BackendModule).Assembly)
            .Where(t => singletons.All(s => s.GetType() != t))
            .AsImplementedInterfaces()
            .InstancePerDependency();

        foreach (var singleton in singletons)
        {
            builder.RegisterInstance(singleton).AsImplementedInterfaces();
        }
    }
}
