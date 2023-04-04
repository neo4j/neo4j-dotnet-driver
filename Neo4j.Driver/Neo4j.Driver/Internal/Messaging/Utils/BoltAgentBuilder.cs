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

#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Neo4j.Driver.Internal.Messaging.Utils;

internal static class BoltAgentBuilder
{
    public static string GetBoltAgent()
    {
        var version = typeof(BoltAgentBuilder).Assembly.GetName().Version;
        if (version == null)
        {
            throw new ClientException("Could not collect assembly version of driver required for handshake.");
        }

        var os = OsString();
        var env = DotnetString();

        return $"neo4j-dotnet/{version.Major}.{version.Minor}.{version.Build}{os}{env}";
    }

    private static string DotnetString()
    {
        try
        {
            var regex = new Regex(@"\.net ([a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            var framework = regex.Replace(RuntimeInformation.FrameworkDescription, ".NET-$1").Replace(' ', '/');
            return $" {framework}";
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static string OsString()
    {
        try
        {
            var arch = RuntimeInformation.ProcessArchitecture;
            var os = RuntimeInformation.OSDescription;
            return $" ({os};{arch})";
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
