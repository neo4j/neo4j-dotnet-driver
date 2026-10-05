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

using Moq.AutoMock;
using NUnit.Framework;
using Serilog;
using Serilog.Extensions.Logging;

namespace Neo4j.Driver.Bolt.Tests;

public class UnitTestBase<T> where T : class
{
    protected AutoMocker AutoMocker = new();

    private Lazy<T>? _subject;
    protected T Subject => _subject?.Value ?? throw new InvalidOperationException("Subject not initialized.");

    [SetUp]
    public void SetUpBase()
    {
        AutoMocker = new AutoMocker();
        _subject = new Lazy<T>(() => AutoMocker.CreateInstance<T>());
        var logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Debug().CreateLogger();
        var frameworkLogger = new SerilogLoggerProvider(logger).CreateLogger("Neo4j.Driver.Bolt.Tests");
        AutoMocker.Use(frameworkLogger);
    }
}
