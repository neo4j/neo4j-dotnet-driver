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


internal interface IQueryApiTransactionScope : IDisposable
{
    IScopedTransaction Transaction();
}

internal partial class QueryApiSessionComposition : IQueryApiTransactionScope
{
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Used to setup DI")]
    private static void Setup()
    {
        DI.Setup(nameof(QueryApiSessionComposition))
            .Hint(Hint.Resolve, "Off")
            .DependsOn(QueryApiCommonBindings.Name)
            .Arg<SessionConfig>("sessionConfig")
            .Arg<IAuthTokenManager>("authTokenManager")
            .Arg<ILoggingContextTracker>("sessionTracker")
            .Arg<ILoggerFactory>("loggerFactory")
            .Arg<IQueryApiHttpTransport>("httpTransport")
            .Arg<IServerInfo>("serverInfo")

            .Bind<DriverContext>().To((SessionConfig c) => c.DriverContext)
            .Bind<ISessionContext>().As(Singleton).To<QueryApiSessionContext>()
            .Bind<IBookmarkTracker>().As(Singleton).To<BookmarkTracker>()

            .Bind<IAutoCommitRunner>().To<AutoCommitRunner>()
            .Bind<IQueryApiTransactionFactory>().To<QueryApiTransactionFactory>()
            .Bind<IQueryApiTransactionScope>().To((QueryApiSessionComposition c) => new QueryApiSessionComposition(c))

            .Bind<IInternalAsyncSession>().To<QueryApiSession>()
            .Root<IInternalAsyncSession>("Session", kind: RootKinds.Public | RootKinds.Method)

            .Bind<IQueryApiTransactionContextTracker>().As(Scoped).To<QueryApiTransactionContextTracker>()
            .Bind<IHttpRequestEnricher>(nameof(QueryApiClusterAffinityEnricher))
            .To<QueryApiClusterAffinityEnricher>()

            .Bind<IClusterAffinityExtractor>().To<QueryApiClusterAffinityExtractor>()
            .Bind<ITransactionBeginner>().To<TransactionBeginner>()
            .Bind<ITransactionCommitter>().To<TransactionCommitter>()
            .Bind<ITransactionRollback>().To<TransactionRollbacker>()
            .Bind<ITransactionRunner>().To<TransactionRunner>()

            .Bind<IScopedTransaction>().To<QueryApiTransaction>()
            .Root<IScopedTransaction>("Transaction", kind: RootKinds.Public | RootKinds.Method);
    }
}
