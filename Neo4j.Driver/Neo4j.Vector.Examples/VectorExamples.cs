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

using Neo4j.Driver.Mapping;

namespace Neo4j.Vector.Examples;

using System;
using Xunit;
using FluentAssertions;
using Driver;

public class VectorExamplesTests : IDisposable
{
    private readonly IDriver? _driver;

    public VectorExamplesTests()
    {
        _driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "pass"));
        _driver.ExecutableQuery("MATCH (n) DETACH DELETE n").ExecuteAsync().GetAwaiter().GetResult();
        RecordObjectMapping.Reset();
    }

    public void Dispose()
    {
        _driver?.Dispose();
    }

    [Fact]
    public async Task ShouldWriteAndReadVector()
    {
        // create vector
        var vectorElements = new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var doubleVector = new Vector<double>(vectorElements);

        // write node with vector
        await _driver!
            .ExecutableQuery("CREATE (n:ShouldWriteAndReadVector {vector: $vector}) RETURN n")
            .WithParameters(new { vector = doubleVector })
            .ExecuteAsync();

        // read node with vector
        var result = await _driver
            .ExecutableQuery("MATCH (n:ShouldWriteAndReadVector) RETURN n")
            .ExecuteAsync();

        var record = result.Result[0];
        var node = (INode)record["n"];

        // Here, we expect an array of doubles.
        node.Properties.Should().ContainKey("vector");
        var vectorValue = node.Properties["vector"];

        vectorValue.Should().BeOfType<Vector<double>>();
        var vector = (Vector<double>)vectorValue;
        vector.Values.Should().Equal(vectorElements);
    }

    [Fact]
    public async Task ShouldWriteAndReadCSharpRecordWithVectors()
    {
        // create C# object with vectors
        var record = new
        {
            DoubleVector = new Vector<double>([0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0]),
            LongVector = new Vector<long>([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10])
        };

        // write node with vectors
        await _driver!
            .ExecutableQuery("CREATE (n:ShouldWriteAndReadCSharpRecordWithVectors $record) RETURN n")
            .WithParameters(new { record })
            .ExecuteAsync();

        // read node back
        var eagerResult = await _driver
            .ExecutableQuery("MATCH (n:ShouldWriteAndReadCSharpRecordWithVectors) RETURN n")
            .ExecuteAsync();

        var node = (INode)eagerResult.Result[0]["n"];

        var doubleVectorRead = (Vector<double>)node.Properties["DoubleVector"];
        var longVectorRead = (Vector<long>)node.Properties["LongVector"];

        doubleVectorRead.Should().BeEquivalentTo(record.DoubleVector);
        longVectorRead.Should().BeEquivalentTo(record.LongVector);
    }

    public class ClassForMappingWithVector(Vector<double> doubleVector, Vector<long> longVector)
    {
        public Vector<double> DoubleVector { get; } = doubleVector;
        public Vector<long> LongVector { get; } = longVector;
    }

    [Fact]
    public async Task ShouldWorkWithObjectMapping()
    {
        // create C# vectors
        var doubleVector = new Vector<double>([0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0]);
        var longVector = new Vector<long>([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        // write node with vectors
        await _driver!
            .ExecutableQuery("CREATE (n:ShouldWorkWithObjectMapping $record) RETURN n")
            .WithParameters(new { record = new { doubleVector, longVector } })
            .ExecuteAsync();


        // read node with vector
        var result = await _driver
            .ExecutableQuery(
                """
                    MATCH (n:ShouldWorkWithObjectMapping) 
                    RETURN n.doubleVector AS doubleVector, n.longVector AS longVector
                """)
            .ExecuteAsync()
            .AsObjectsAsync<ClassForMappingWithVector>();

        var record = result[0];
        record.DoubleVector.Should().BeEquivalentTo(doubleVector);
        record.LongVector.Should().BeEquivalentTo(longVector);
    }

}
