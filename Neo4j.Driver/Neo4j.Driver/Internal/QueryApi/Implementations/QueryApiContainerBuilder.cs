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

using System.Linq;
using Neo4j.Driver.Internal.DependencyInjection;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

[AutoRegister]
internal class QueryApiContainerBuilder : IQueryApiContainerBuilder
{
    public IServiceResolver BuildContainer(DriverContext driverContext)
    {
        var container = new ScopedContainerRewrite();
        
        container.RegisterInstance(driverContext);
        container.RegisterInstance(driverContext.AuthTokenManager);

        var loggerFactory = new LoggerFactory(driverContext.Neo4JLogger);
        var loggerPlugin = new LoggerResolverOverride(loggerFactory);
        container.RegisterPlugin(loggerPlugin);

        var serverInfo = new QueryApiServerInfo(driverContext.InitialUri);
        container.RegisterInstance<IServerInfo>(serverInfo);
        container.RegisterInstance(serverInfo);
        
        // find types in this assembly that are marked with the AutoRegister attribute
        var types = typeof(QueryApiContainerBuilder)
            .Assembly
            .GetTypes()
            .Where(t => 
                t.IsClass 
                && t.GetCustomAttributes(typeof(AutoRegisterAttribute), false).Length != 0)
            .ToList();
        
        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces();
            foreach (var ifc in interfaces)
            {
                container.RegisterType(ifc, type);
            }
        }
        
        return container;
    }
}
