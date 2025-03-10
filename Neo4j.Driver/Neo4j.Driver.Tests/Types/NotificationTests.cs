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

using FluentAssertions;
using Neo4j.Driver.Internal.Result;
using Newtonsoft.Json;
using Xunit;

namespace Neo4j.Driver.Tests.Types;

public class NotificationTests
{
    [Fact]
    public void ShouldSerialize()
    {
        var notification = new Notification(
            "code",
            "title",
            "description",
            new InputPosition(0, 1, 2),
            "WARNING",
            "HINT");

        var text = JsonConvert.SerializeObject(notification, Formatting.Indented).ReplaceLineEndings();

        text.Should()
            .BeEquivalentTo(
                """
                    {
                      "RawSeverityLevel": "WARNING",
                      "SeverityLevel": 1,
                      "RawCategory": "HINT",
                      "Category": 1,
                      "Code": "code",
                      "Title": "title",
                      "Description": "description",
                      "Position": {
                        "Offset": 0,
                        "Line": 1,
                        "Column": 2
                      },
                      "Severity": "WARNING"
                    }
                    """.ReplaceLineEndings());
    }
}
