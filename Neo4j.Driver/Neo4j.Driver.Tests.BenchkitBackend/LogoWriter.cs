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

namespace Neo4j.Driver.Tests.BenchkitBackend;

internal static class LogoWriter
{
    public static void WriteLogo()
    {
        Console.WriteLine(
            """
               ___               __   __    _ __   
              / _ )___ ___  ____/ /  / /__ (_) /_  
             / _  / -_) _ \/ __/ _ \/  '_// / __/  
            /____/\__/_//_/\__/_//_/_/\_\/_/\__/ __
                 / _ )___ _____/ /_____ ___  ___/ /
                / _  / _ `/ __/  '_/ -_) _ \/ _  / 
               /____/\_,_/\__/_/\_\\__/_//_/\_,_/ 
                                                  v1.0.0
                 
            """);
    }
}
