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

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace Neo4j.Driver.Internal.QueryApi;

internal interface IQueryApiDriverComposition : IAsyncDisposable;

internal partial class QueryApiDriverComposition : IQueryApiDriverComposition
{
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Used to setup DI")]
    private static void Setup()
    {
        DI.Setup(nameof(QueryApiDriverComposition))
            .Hint(Hint.Resolve, "Off")
            .DependsOn(QueryApiCommonBindings.Name)
            .Arg<DriverContext>("driverContext")

            .Bind<IQueryApiDriverComposition>().To((QueryApiDriverComposition c) => c)

            .Bind<ILoggerFactory>().As(Singleton).To((DriverContext c) => new LoggerFactory(c.Neo4JLogger))

            .Bind<ILoggingContextTracker>().As(Singleton).To<LoggingContextTracker>()

            .Bind<IServerInfo>().Bind<IServerAgentWriter>().As(Singleton).To<QueryApiServerInfo>()

            .Bind<IConnectivityVerifier>().To<ConnectivityVerifier>()
            .Bind<IQueryApiHttpTransport>().As(Singleton).To<QueryApiHttpTransport>()
            .Bind<IQueryApiSessionFactory>().To<QueryApiSessionFactory>()
            .Bind<ISessionIdGenerator>().To<SessionIdGenerator>()

            .Bind<IQueryApiProtocolAdapter>().To<QueryApiProtocolAdapter>()
            .Root<IQueryApiProtocolAdapter>("ProtocolAdapter", kind: RootKinds.Public | RootKinds.Method);
    }
}
