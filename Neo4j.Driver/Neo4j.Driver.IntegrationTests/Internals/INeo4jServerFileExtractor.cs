// Copyright (c) 2002-2016 "Neo Technology,"
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
namespace Neo4j.Driver.IntegrationTests.Internals
{
    /// <summary>
    ///     File extractor
    /// </summary>
    internal interface INeo4jServerFileExtractor
    {
        /// <summary>
        ///     Extract the given file.
        /// </summary>
        /// <param name="fileToExtractPath">The file to extract.</param>
        /// <param name="targetFolder">Target folder to extract to.</param>
        /// <param name="fileWasNewlyDownloaded">This is true if the file was just downloaded. Else false.</param>
        /// <returns>The final extracted folder, that is the base folder for the Neo4j server.</returns>
        string ExtractFile(string fileToExtractPath, string targetFolder, bool fileWasNewlyDownloaded);
    }
}