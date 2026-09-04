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

using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal;

/// <summary>
/// A transaction whose per-transaction state lives in a DI scope: the owning factory begins it,
/// and disposes the scope when <see cref="IAsyncNotifyingDisposable.Disposed"/> fires.
/// </summary>
internal interface IScopedTransaction : IInternalAsyncTransaction, IAsyncNotifyingDisposable
{
    Task BeginAsync(CancellationToken cancellationToken = default);
}
