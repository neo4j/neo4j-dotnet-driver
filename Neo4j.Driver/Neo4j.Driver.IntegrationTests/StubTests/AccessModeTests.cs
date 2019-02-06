// Copyright (c) 2002-2019 "Neo4j,"
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.V1;
using Xunit;

namespace Neo4j.Driver.IntegrationTests.StubTests
{
    public class AccessModeTests
    {
        private static readonly Config NoEncryption =
            Config.Builder.WithEncryptionLevel(EncryptionLevel.None).ToConfig();

        [RequireBoltStubServerFactAttribute]
        public void RunOnReadModeSessionShouldGoToReader()
        {
            using (BoltStubServer.Start("accessmode_router", 9001))
            {
                using (BoltStubServer.Start("accessmode_reader_implicit", 9003))
                {
                    using (var driver =
                        GraphDatabase.Driver("bolt+routing://localhost:9001", AuthTokens.None, NoEncryption))
                    {
                        using (var session = driver.Session(AccessMode.Read))
                        {
                            var list = session.Run("RETURN $x", new {x = 1}).Select(r => Convert.ToInt32(r[0]))
                                .ToList();

                            list.Should().HaveCount(1).And.Contain(1);
                        }
                    }
                }
            }
        }

        [RequireBoltStubServerFactAttribute]
        public void RunOnReadModeTransactionShouldGoToReader()
        {
            using (BoltStubServer.Start("accessmode_router", 9001))
            {
                using (BoltStubServer.Start("accessmode_reader_explicit", 9003))
                {
                    using (var driver =
                        GraphDatabase.Driver("bolt+routing://localhost:9001", AuthTokens.None, NoEncryption))
                    {
                        using (var session = driver.Session(AccessMode.Read))
                        {
                            using (var tx = session.BeginTransaction())
                            {
                                var list = tx.Run("RETURN $x", new {x = 1}).Select(r => Convert.ToInt32(r[0]))
                                    .ToList();

                                list.Should().HaveCount(1).And.Contain(1);

                                tx.Success();
                            }
                        }
                    }
                }
            }
        }

        [RequireBoltStubServerTheoryAttribute]
        [InlineData(AccessMode.Read)]
        [InlineData(AccessMode.Write)]
        public void ReadTransactionOnSessionShouldGoToReader(AccessMode mode)
        {
            using (BoltStubServer.Start("accessmode_router", 9001))
            {
                using (BoltStubServer.Start("accessmode_reader_func", 9003))
                {
                    using (var driver =
                        GraphDatabase.Driver("bolt+routing://localhost:9001", AuthTokens.None, NoEncryption))
                    {
                        using (var session = driver.Session(mode))
                        {
                            var list = session.ReadTransaction(tx => tx.Run("RETURN $x", new {x = 1})
                                .Select(r => Convert.ToInt32(r[0]))
                                .ToList());

                            list.Should().HaveCount(1).And.Contain(1);
                        }
                    }
                }
            }
        }

        [RequireBoltStubServerFactAttribute]
        public void RunOnWriteModeSessionShouldGoToWriter()
        {
            using (BoltStubServer.Start("accessmode_router", 9001))
            {
                using (BoltStubServer.Start("accessmode_writer_implicit", 9002))
                {
                    using (var driver =
                        GraphDatabase.Driver("bolt+routing://localhost:9001", AuthTokens.None, NoEncryption))
                    {
                        using (var session = driver.Session(AccessMode.Write))
                        {
                            var list = session.Run("CREATE (n: { id: $x }) RETURN $x", new { x = 1 }).Select(r => Convert.ToInt32(r[0]))
                                .ToList();

                            list.Should().HaveCount(1).And.Contain(1);
                        }
                    }
                }
            }
        }

        [RequireBoltStubServerFactAttribute]
        public void RunOnWriteModeTransactionShouldGoToReader()
        {
            using (BoltStubServer.Start("accessmode_router", 9001))
            {
                using (BoltStubServer.Start("accessmode_writer_explicit", 9002))
                {
                    using (var driver =
                        GraphDatabase.Driver("bolt+routing://localhost:9001", AuthTokens.None, NoEncryption))
                    {
                        using (var session = driver.Session(AccessMode.Write))
                        {
                            using (var tx = session.BeginTransaction())
                            {
                                var list = tx.Run("CREATE (n: { id: $x }) RETURN $x", new { x = 1 }).Select(r => Convert.ToInt32(r[0]))
                                    .ToList();

                                list.Should().HaveCount(1).And.Contain(1);

                                tx.Success();
                            }
                        }
                    }
                }
            }
        }

        [RequireBoltStubServerTheoryAttribute]
        [InlineData(AccessMode.Read)]
        [InlineData(AccessMode.Write)]
        public void WriteTransactionOnSessionShouldGoToReader(AccessMode mode)
        {
            using (BoltStubServer.Start("accessmode_router", 9001))
            {
                using (BoltStubServer.Start("accessmode_writer_func", 9002))
                {
                    using (var driver =
                        GraphDatabase.Driver("bolt+routing://localhost:9001", AuthTokens.None, NoEncryption))
                    {
                        using (var session = driver.Session(mode))
                        {
                            var list = session.WriteTransaction(tx => tx.Run("CREATE (n: { id: $x }) RETURN $x", new { x = 1 })
                                .Select(r => Convert.ToInt32(r[0]))
                                .ToList());

                            list.Should().HaveCount(1).And.Contain(1);
                        }
                    }
                }
            }
        }

    }
}