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

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Neo4j.Driver.Tests.Filters;

public class MonoFactAttribute : FactAttribute
{
    public MonoFactAttribute()
    {
        var shouldSkip = Type.GetType("Mono.Runtime") == null;

        if (shouldSkip)
        {
            Skip = "Test is supposed to be run only on mono runtimes";
        }
    }
}

public class MonoTheoryAttribute : TheoryAttribute
{
    public MonoTheoryAttribute()
    {
        var shouldSkip = Type.GetType("Mono.Runtime") == null;

        if (shouldSkip)
        {
            Skip = "Test is supposed to be run only on mono runtimes";
        }
    }
}

public class DotnetCoreFactAttribute : FactAttribute
{
    public DotnetCoreFactAttribute()
    {
        var shouldSkip =
            RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase) ==
            false;

        if (shouldSkip)
        {
            Skip = "Test is supposed to be run only on .net core runtimes";
        }
    }
}

public class DotnetCoreTheoryAttribute : TheoryAttribute
{
    public DotnetCoreTheoryAttribute()
    {
        var shouldSkip =
            RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase) ==
            false;

        if (shouldSkip)
        {
            Skip = "Test is supposed to be run only on .net core runtimes";
        }
    }
}
