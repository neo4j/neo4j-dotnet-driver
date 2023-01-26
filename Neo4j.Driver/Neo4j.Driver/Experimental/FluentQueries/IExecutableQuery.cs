// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License"):
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4j.Driver.Experimental.FluentQueries;

/// <summary>
/// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
/// </summary>
public interface IExecutableQuery
{
    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// </summary>
    IExecutableQuery WithConfig(QueryConfig config);

    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// </summary>
    IExecutableQuery WithParameters(object parameters);

    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// </summary>
    IExecutableQuery WithParameters(Dictionary<string, object> parameters);

    // removing since behaviour is different to WithParameters, pending discussion
    // IExecutableQuery WithParameter(string name, object value);

    /// <summary>
    /// There is no guarantee that anything in Neo4j.Driver.Experimental namespace will be in a next minor version.
    /// </summary>
    Task<EagerResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
