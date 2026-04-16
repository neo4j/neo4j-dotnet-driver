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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using FluentAssertions;
using Xunit;

namespace Neo4j.Driver.Tests.CompileTimeChecks;

public class ReturnCursorCompileTests
{
    private static CSharpCompilation Compile(string source)
    {
        var references = CollectReferences();
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IEnumerable<MetadataReference> CollectReferences()
    {
        // Driver assembly
        yield return MetadataReference.CreateFromFile(typeof(IAsyncSession).Assembly.Location);

        // .NET runtime assemblies
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        foreach (var name in new[]
                 {
                     "System.Runtime.dll",
                     "System.Private.CoreLib.dll",
                     "System.Collections.dll",
                     "netstandard.dll"
                 })
        {
            var path = Path.Combine(runtimeDir, name);
            if (File.Exists(path))
                yield return MetadataReference.CreateFromFile(path);
        }

        // Task / async support
        yield return MetadataReference.CreateFromFile(typeof(Task).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(Task<>).Assembly.Location);
    }

    [Fact]
    public void ReturningCursorFromTransactionFunction_ProducesWarning()
    {
        var source = """
            using System.Threading.Tasks;
            using Neo4j.Driver;

            class Test
            {
                async Task Run(IAsyncSession session)
                {
                    var cursor = await session.ExecuteReadAsync(async tx =>
                        await tx.RunAsync("RETURN 1"));
                }
            }
            """;

        var diagnostics = Compile(source).GetDiagnostics();
        diagnostics.Should().Contain(
            d => d.Severity == DiagnosticSeverity.Warning && d.Id == "CS0618",
            because: "returning IResultCursor from a transaction function should produce an obsolete warning");
    }

    [Fact]
    public void ConsumingResultsInsideDelegate_Compiles()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Neo4j.Driver;

            class Test
            {
                async Task<List<IRecord>> Run(IAsyncSession session)
                {
                    return await session.ExecuteReadAsync(async tx =>
                    {
                        var cursor = await tx.RunAsync("RETURN 1");
                        return await cursor.ToListAsync();
                    });
                }
            }
            """;

        var diagnostics = Compile(source).GetDiagnostics();
        diagnostics.Should().NotContain(
            d => d.Severity == DiagnosticSeverity.Error,
            because: "consuming results inside the delegate is valid usage");
    }
}
