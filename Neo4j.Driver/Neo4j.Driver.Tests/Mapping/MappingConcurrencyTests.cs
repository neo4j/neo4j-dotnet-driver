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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Mapping;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.Tests.Mapping;

public class MappingConcurrencyTests(ITestOutputHelper testOutputHelper)
{
    private interface ITestTask
    {
        Task Start();
    }

    private class TestTask<T> : ITestTask
    {
        public Task Start()
        {
            return Task.Run(
                () =>
                {
                    for (var i = 0; i < 50; i++)
                    {
                        DefaultMapper.Get<T>();
                        DefaultMapper.Reset();
                    }
                });
        }
    }

    private record DummyType1(string Name, int Age);
    private record DummyType2(string Name, int Age);
    private record DummyType3(string Name, int Age);
    private record DummyType4(string Name, int Age);

    [Fact]
    public async void DefaultMapperShouldBeThreadSafe()
    {
        List<ITestTask> threads =
        [
            new TestTask<DummyType1>(),
            new TestTask<DummyType2>(),
            new TestTask<DummyType3>(),
            new TestTask<DummyType4>()
        ];

        // wait for all threads to finish
        await Task.WhenAll(threads.Select(t => t.Start()));

        testOutputHelper.WriteLine("All threads finished.");
    }

    [Fact]
    public async Task MapMethods_ShouldBeThreadSafe()
    {
        var testObjects = CreateMultiTypedArray(100);

        const int numberOfThreads = 4;
        var tasks = new List<Task>(numberOfThreads);
        var resetEvent = new ManualResetEventSlim(false);

        for (var i = 0; i < numberOfThreads; i++)
        {
            tasks.Add(
                Task.Run(() =>
                {
                    resetEvent.Wait(); // Wait for the signal to start
                    for (var j = 0; j < 100; j++)
                    {
                        foreach (var obj in testObjects)
                        {
                            obj.AsType(obj.GetType());
                        }

                        MappingExtensions.ResetAsMethods();
                    }
                }));
        }

        resetEvent.Set(); // Signal all tasks to start
        await Task.WhenAll(tasks);
    }

    private static object[] CreateMultiTypedArray(int numValues)
    {
        return Enumerable.Range(0, numValues)
            .Select(_ => GetUniquelyTypedValue())
            .ToArray();
    }

    private static object GetUniquelyTypedValue()
    {
        var typeName = $"Type_{Guid.NewGuid()}";

        var asmName = new AssemblyName($"Asm_{Guid.NewGuid()}");
        var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(
            asmName,
            AssemblyBuilderAccess.Run);

        var moduleBuilder = asmBuilder.DefineDynamicModule($"Module{Guid.NewGuid()}");

        // Create a completely empty public class
        var typeBuilder = moduleBuilder.DefineType(
            typeName,
            TypeAttributes.Public | TypeAttributes.Class);

        Type emittedType = typeBuilder.CreateTypeInfo();

        // Instantiate it
        var instance = Activator.CreateInstance(emittedType);
        return instance;
    }
}
