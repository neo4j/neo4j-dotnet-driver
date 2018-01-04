// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.Tests.IO
{
    public class PackStreamReaderBytesIncompatibleTests
    {

        [Fact]
        public void ShouldThrowWhenBytesIsSent()
        {
            var mockInput =
                IOExtensions.CreateMockStream("CC 01 01".ToByteArray());
            var reader = new PackStreamReaderBytesIncompatible(mockInput.Object, BoltReader.StructHandlers);

            var ex = Record.Exception(() => reader.Read());

            ex.Should().NotBeNull();
            ex.Should().BeOfType<ProtocolException>();
        }

    }
}
