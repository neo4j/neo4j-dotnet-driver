// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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

using System;
using System.Runtime.InteropServices;

namespace Neo4j.Driver.Internal
{
    internal static class RuntimeHelper
    {
        private static readonly string FrameworkDescription;

        static RuntimeHelper()
        {
#if NET452
            FrameworkDescription = ".NET Framework";
#else
            FrameworkDescription = RuntimeInformation.FrameworkDescription ?? string.Empty;
#endif
        }

        public static bool IsMono()
        {
            return FrameworkDescription.StartsWith("mono", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDotnetFramework()
        {
            return FrameworkDescription.StartsWith(".net framework", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDotnetCore()
        {
            // there are entries that RuntimeInformation.FrameworkDescription returns null
            // so we will rely on not being IsMono and IsDotnetFramework
            return !IsMono() && !IsDotnetFramework();
        }
    }
}