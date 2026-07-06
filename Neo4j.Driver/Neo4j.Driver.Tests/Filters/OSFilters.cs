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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Filters;

public class OSFactAttribute : FactAttribute
{
    public OSFactAttribute(
        OSPlatform[] onPlatforms,
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (onPlatforms.All(platform => !RuntimeInformation.IsOSPlatform(platform)))
            Skip = $"Test is supposed to be run only on platforms '{onPlatforms.ToContentString()}'";
    }
}

public class WindowsFactAttribute : OSFactAttribute
{
    public WindowsFactAttribute(
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base([OSPlatform.Windows], sourceFilePath, sourceLineNumber)
    {
    }
}

public class LinuxFactAttribute : OSFactAttribute
{
    public LinuxFactAttribute(
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base([OSPlatform.Linux], sourceFilePath, sourceLineNumber)
    {
    }
}

public class OSXFactAttribute : OSFactAttribute
{
    public OSXFactAttribute(
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base([OSPlatform.OSX], sourceFilePath, sourceLineNumber)
    {
    }
}

public class UnixFactAttribute : OSFactAttribute
{
    public UnixFactAttribute(
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base([OSPlatform.Linux, OSPlatform.OSX], sourceFilePath, sourceLineNumber)
    {
    }
}

public class OSTheoryAttribute : TheoryAttribute
{
    public OSTheoryAttribute(
        OSPlatform[] onPlatforms,
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (onPlatforms.All(platform => !RuntimeInformation.IsOSPlatform(platform)))
            Skip = $"Test is supposed to be run only on platforms '{onPlatforms.ToContentString()}'";
    }
}

public class WindowsTheoryAttribute : OSTheoryAttribute
{
    public WindowsTheoryAttribute(
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base([OSPlatform.Windows], sourceFilePath, sourceLineNumber)
    {
    }
}

public class LinuxTheoryAttribute : OSTheoryAttribute
{
    public LinuxTheoryAttribute(
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base([OSPlatform.Linux], sourceFilePath, sourceLineNumber)
    {
    }
}

public class OSXTheoryAttribute : OSTheoryAttribute
{
    public OSXTheoryAttribute(
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base([OSPlatform.OSX], sourceFilePath, sourceLineNumber)
    {
    }
}

public class UnixTheoryAttribute : OSTheoryAttribute
{
    public UnixTheoryAttribute(
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base([OSPlatform.Linux, OSPlatform.OSX], sourceFilePath, sourceLineNumber)
    {
    }
}
