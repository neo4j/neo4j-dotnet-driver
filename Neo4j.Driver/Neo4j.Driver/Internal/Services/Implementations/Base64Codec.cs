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

namespace Neo4j.Driver.Internal;

[DriverAutoRegister(singleton: true)]
internal class Base64Codec : IBase64Codec
{
    public string Encode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes);
    }

    public byte[] Decode(string text)
    {
        return Convert.FromBase64String(text);
    }
}
