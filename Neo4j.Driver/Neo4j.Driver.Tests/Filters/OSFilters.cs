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
using System.Runtime.InteropServices;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Filters;

public class OSFactAttribute : FactAttribute
{
    public OSFactAttribute(params OSPlatform[] onPlatforms)
    {
        var shouldSkip = onPlatforms.All(platform => !RuntimeInformation.IsOSPlatform(platform));

        if (shouldSkip)
        {
            Skip = $"Test is supposed to be run only on platforms '{onPlatforms.ToContentString()}'";
        }
    }
}

public class WindowsFactAttribute : OSFactAttribute
{
    public WindowsFactAttribute() : base(OSPlatform.Windows)
    {
    }
}

public class LinuxFactAttribute : OSFactAttribute
{
    public LinuxFactAttribute() : base(OSPlatform.Linux)
    {
    }
}

public class OSXFactAttribute : OSFactAttribute
{
    public OSXFactAttribute() : base(OSPlatform.OSX)
    {
    }
}

public class UnixFactAttribute : OSFactAttribute
{
    public UnixFactAttribute() : base(OSPlatform.Linux, OSPlatform.OSX)
    {
    }
}

public class OSTheoryAttribute : TheoryAttribute
{
    public OSTheoryAttribute(params OSPlatform[] onPlatforms)
    {
        var shouldSkip = onPlatforms.All(platform => !RuntimeInformation.IsOSPlatform(platform));

        if (shouldSkip)
        {
            Skip = $"Test is supposed to be run only on platforms '{onPlatforms.ToContentString()}'";
        }
    }
}

public class WindowsTheoryAttribute : OSTheoryAttribute
{
    public WindowsTheoryAttribute() : base(OSPlatform.Windows)
    {
    }
}

public class LinuxTheoryAttribute : OSTheoryAttribute
{
    public LinuxTheoryAttribute() : base(OSPlatform.Linux)
    {
    }
}

public class OSXTheoryAttribute : OSTheoryAttribute
{
    public OSXTheoryAttribute() : base(OSPlatform.OSX)
    {
    }
}

public class UnixTheoryAttribute : OSTheoryAttribute
{
    public UnixTheoryAttribute() : base(OSPlatform.Linux, OSPlatform.OSX)
    {
    }
}
