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

using System.Diagnostics.CodeAnalysis;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace Neo4j.Driver.Internal.QueryApi;

internal partial class QueryApiTransactionComposition
{
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Used to setup DI")]
    private static void Setup()
    {
        DI.Setup(nameof(QueryApiTransactionComposition))
            .Hint(Hint.Resolve, "Off")
            .DependsOn(QueryApiCommonBindings.Name)
            .Arg<DriverContext>("driverContext")
            .Arg<ISessionContext>("sessionContext")
            .Arg<IAuthTokenManager>("authTokenManager")
            .Arg<ILoggerFactory>("loggerFactory")
            .Arg<ILoggingContextTracker>("sessionTracker")
            .Arg<IQueryApiHttpTransport>("httpTransport")
            .Arg<IServerInfo>("serverInfo")
            .Arg<IBookmarkTracker>("bookmarkTracker")

            .Bind<IQueryApiTransactionContextTracker>().As(Singleton).To<QueryApiTransactionContextTracker>()
            .Bind<IHttpRequestEnricher>(nameof(QueryApiClusterAffinityEnricher)).To<QueryApiClusterAffinityEnricher>()
            .Bind<IClusterAffinityExtractor>().To<QueryApiClusterAffinityExtractor>()
            .Bind<ITransactionBeginner>().To<TransactionBeginner>()
            .Bind<ITransactionCommitter>().To<TransactionCommitter>()
            .Bind<ITransactionRollback>().To<TransactionRollbacker>()
            .Bind<ITransactionRunner>().To<TransactionRunner>()

            .Bind<IScopedTransaction>().To<QueryApiTransaction>()
            .Root<IScopedTransaction>("Transaction", kind: RootKinds.Public | RootKinds.Method);
    }
}
