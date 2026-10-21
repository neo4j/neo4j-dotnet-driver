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

using System.Diagnostics;
using System.Reflection;

namespace Neo4j.Driver.TestKitBackend.Errors;

internal interface IExceptionOriginClassifier
{
    bool OriginatesInDriver(Exception exception);
}

internal class ExceptionOriginClassifier : IExceptionOriginClassifier
{
    private static readonly Assembly DriverAssembly = typeof(IDriver).Assembly;
    private static readonly Assembly BackendAssembly = typeof(ExceptionOriginClassifier).Assembly;

    public bool OriginatesInDriver(Exception exception)
    {
        if (exception is Neo4jException)
        {
            return true;
        }

        foreach (var frame in new StackTrace(exception, false).GetFrames())
        {
            var assembly = frame.GetMethod()?.DeclaringType?.Assembly;
            if (assembly == DriverAssembly)
            {
                return true;
            }

            if (assembly == BackendAssembly)
            {
                return false;
            }
        }

        return false;
    }
}
