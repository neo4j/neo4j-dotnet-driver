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

using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal interface IRemainingPropertiesMapper
{
    void Apply<TBuilder>(object request, TBuilder builder, IReadOnlySet<string> handledExplicitly)
        where TBuilder : notnull;
}

internal class RemainingPropertiesMapper : IRemainingPropertiesMapper
{
    public void Apply<TBuilder>(object request, TBuilder builder, IReadOnlySet<string> handledExplicitly)
        where TBuilder : notnull
    {
        foreach (var property in request.GetType().GetProperties())
        {
            if (handledExplicitly.Contains(property.Name))
            {
                continue;
            }

            var value = property.GetValue(request);
            if (value is null)
            {
                continue;
            }

            var (methodName, argument) = property.Name.EndsWith("Ms", StringComparison.Ordinal)
                ? ("With" + property.Name[..^2], (object)TimeSpan.FromMilliseconds((long)value))
                : ("With" + property.Name, value);

            var method = typeof(TBuilder).GetMethod(methodName) ??
                throw new InvalidOperationException(
                    $"No {methodName} method found on {typeof(TBuilder).Name} for {property.Name}.");

            try
            {
                method.Invoke(builder, [argument]);
            }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }
    }
}
