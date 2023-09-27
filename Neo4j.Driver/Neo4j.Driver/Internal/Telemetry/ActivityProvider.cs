// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System.Diagnostics;

namespace Neo4j.Driver.Internal.Telemetry;

internal interface IActivityProvider
{
    /// <summary>
    /// Creates a new activity object using the specified name and activity kind.
    /// </summary>
    /// <param name="name">The operation name of the activity.</param>
    /// <param name="kind">The activity kind.</param>
    /// <returns></returns>
    public Activity CreateActivity(string name, ActivityKind kind);
}

internal class ActivityProvider : IActivityProvider
{
    public static readonly ActivityProvider Default = new("Neo4j.Driver");

    private readonly ActivitySource _activitySource;

    public ActivityProvider(string name)
    {
        _activitySource = new ActivitySource(name);
    }

    /// <inheritdoc />
    public Activity CreateActivity(string name, ActivityKind kind)
    {
        return _activitySource.CreateActivity(name, kind);
    }
}
