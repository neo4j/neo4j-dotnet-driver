// Copyright (c) 2002-2022 "Neo4j,"
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

namespace Neo4j.Driver.Tests.TestBackend;

public class DateTimeParameterValue
{
    public int? year { get; set; }
    public int? month { get; set; }
    public int? day { get; set; }
    public int? hour { get; set; }
    public int? minute { get; set; }
    public int? second { get; set; }
    public int? nanosecond { get; set; }
    public int? utc_offset_s { get; set; }
    public string timezone_id { get; set; }
}