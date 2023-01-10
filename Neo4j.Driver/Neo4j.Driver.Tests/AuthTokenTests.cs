// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using FluentAssertions;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class AuthTokenTests
    {
        public class BasicAuthToken
        {
            [Fact]
            public void ShouldCreateBasicAuthTokenWithoutRealm()
            {
                var authToken = AuthTokens.Basic("zhenli", "toufu");
                var dict = authToken.AsDictionary();
                dict.Count.Should().Be(3);
                dict["scheme"].Should().Be("basic");
                dict["principal"].Should().Be("zhenli");
                dict["credentials"].Should().Be("toufu");
                dict.ContainsKey("realm").Should().BeFalse();
            }

            [Fact]
            public void ShouldCreateBasicAuthTokenWithRealm()
            {
                var authToken = AuthTokens.Basic("zhenli", "toufu", "foo");
                var dict = authToken.AsDictionary();
                dict.Count.Should().Be(4);
                dict["scheme"].Should().Be("basic");
                dict["principal"].Should().Be("zhenli");
                dict["credentials"].Should().Be("toufu");
                dict["realm"].Should().Be("foo");
            }
        }

        public class KerberosAuthToken
        {
            [Fact]
            public void ShouldCreateKerberosAuthToken()
            {
                var authToken = AuthTokens.Kerberos("aBase64Str");
                var dict = authToken.AsDictionary();
                dict.Count.Should().Be(3);
                dict["scheme"].Should().Be("kerberos");
                dict["principal"].Should().Be("");
                dict["credentials"].Should().Be("aBase64Str");
            }
        }

        public class CustomAuthToken
        {
            [Fact]
            public void ShouldCreateCustomAuthTokenWithoutParameters()
            {
                var authToken = AuthTokens.Custom("zhenli", "toufu", "foo", "custom");
                var dict = authToken.AsDictionary();
                dict.Count.Should().Be(4);
                dict["scheme"].Should().Be("custom");
                dict["principal"].Should().Be("zhenli");
                dict["credentials"].Should().Be("toufu");
                dict["realm"].Should().Be("foo");
                dict.ContainsKey("parameters").Should().BeFalse();
            }

            [Fact]
            public void ShouldCreateCustomAuthTokenWithParameters()
            {
                var authToken = AuthTokens.Custom(
                    "zhenli",
                    "toufu",
                    "foo",
                    "custom",
                    new Dictionary<string, object>
                    {
                        { "One", 1 },
                        { "Two", 2 },
                        { "Three", 3 }
                    });

                var dict = authToken.AsDictionary();

                dict.Count.Should().Be(5);
                dict["scheme"].Should().Be("custom");
                dict["principal"].Should().Be("zhenli");
                dict["credentials"].Should().Be("toufu");
                dict["realm"].Should().Be("foo");

                var nums = dict["parameters"].As<Dictionary<string, object>>();
                nums["One"].Should().Be(1);
                nums["Two"].Should().Be(2);
                nums["Three"].Should().Be(3);
            }

            [Fact]
            public void ShouldNotAddPrincipalIfNull()
            {
                var authToken = AuthTokens.Custom(null, "toufu", "foo", "custom");
                var dict = authToken.AsDictionary();

                dict.Count.Should().Be(3);
                dict["scheme"].Should().Be("custom");
                dict["credentials"].Should().Be("toufu");
                dict["realm"].Should().Be("foo");

                dict.ContainsKey("parameters").Should().BeFalse();
                dict.ContainsKey("principal").Should().BeFalse();
            }
        }
    }
}
