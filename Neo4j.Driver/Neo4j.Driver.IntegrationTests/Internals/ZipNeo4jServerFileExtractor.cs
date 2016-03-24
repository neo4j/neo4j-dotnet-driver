//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    /// <summary>
    ///     ZIP based implementation of <see cref="INeo4jServerFileExtractor" />.
    /// </summary>
    internal class ZipNeo4jServerFileExtractor : INeo4jServerFileExtractor
    {
        public string ExtractFile(string fileToExtractPath, string targetFolder, bool fileWasNewlyDownloaded)
        {
            var zipFolder = GetZipFolder(fileToExtractPath);
            var destinationFolderPath = Path.Combine(targetFolder, zipFolder);

            if (Directory.Exists(destinationFolderPath))
            {
                if (fileWasNewlyDownloaded)
                {
                    var extractedDir = new DirectoryInfo(destinationFolderPath);
                    extractedDir.Delete(true);
                    ExtractZip(fileToExtractPath, targetFolder);
                }
            }
            else
            {
                ExtractZip(fileToExtractPath, targetFolder);
            }

            return zipFolder;
        }

        private static string GetZipFolder(string filename)
        {
            using (var archive = ZipFile.OpenRead(filename))
            {
                return
                    archive.Entries.Where(a => a.Name == string.Empty).OrderBy(a => a.FullName.Length).First().FullName;
            }
        }

        private static void ExtractZip(string filename, string destinationFolder)
        {
            ZipFile.ExtractToDirectory(filename, destinationFolder);
        }
    }
}