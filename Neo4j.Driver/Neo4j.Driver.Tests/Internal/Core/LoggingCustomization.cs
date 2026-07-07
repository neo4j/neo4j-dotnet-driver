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

using AutoFixture;
using AutoFixture.Kernel;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver.Tests.Internal.Core;

internal class LoggerSpecimenBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is System.Reflection.ParameterInfo p)
        {
            if (p.ParameterType == typeof(ILogger))
            {
                return new TestLogger(p.Member.DeclaringType!);
            }
        }

        return new NoSpecimen();
    }
}

internal class LoggingCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new LoggerSpecimenBuilder());
    }
}
