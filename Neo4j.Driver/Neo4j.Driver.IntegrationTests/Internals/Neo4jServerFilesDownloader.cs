﻿//  Copyright (c) 2002-2016 "Neo Technology,"
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

using System;
using System.IO;
using System.Net;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    internal enum Neo4jServerEdition
    {
        Community,
        Enterprise
    }

    internal enum Neo4jServerPlatform
    {
        Windows,
        Unix
    }

    /// <summary>
    ///     Helper class for downloading Neo4j server files.
    /// </summary>
    internal static class Neo4jServerFilesDownloadHelper
    {
        /// <summary>
        ///     {0}: Edition
        ///     {1}: Version
        ///     {2}: Platform
        ///     {3}: Platform file extension
        /// </summary>
        private static readonly string PackageUrlFormat = "http://alpha.neohq.net/dist/neo4j-{0}-{1}-{2}.{3}";

        private static DirectoryInfo Neo4jDir => new DirectoryInfo("../target/neo4j");

        private static string Version => Environment.GetEnvironmentVariable("version") ?? "3.0.0-NIGHTLY";


        /// <summary>
        ///     Downloads the Neo4j server as a compressed file and saves it to the HDD
        /// </summary>
        /// <param name="edition">The edition to download</param>
        /// <param name="platform">The platform to download</param>
        /// <returns></returns>
        public static DirectoryInfo DownloadNeo4j(Neo4jServerEdition edition, Neo4jServerPlatform platform,
            INeo4jServerFileExtractor fileExtractor)
        {
            EnsureDirectoriesExist();

            var platformFileExtension = GetPlatformFileExtension(platform);
            var downloadFileInfo = new FileInfo($"../target/{Version}.{platformFileExtension}");
            if (downloadFileInfo.Directory != null)
            {
                if (!downloadFileInfo.Directory.Exists)
                    downloadFileInfo.Directory.Create();
            }

            var packageUrl = string.Format(
                PackageUrlFormat,
                edition.ToString().ToLower(),
                Version,
                platform.ToString().ToLower(),
                platformFileExtension
            );

            var downloadedNew = false;
            long expectedSize;
            using (var client = new WebClient())
            {
                client.OpenRead(packageUrl);
                expectedSize = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                if (!downloadFileInfo.Exists || downloadFileInfo.Length != expectedSize)
                {
                    client.DownloadProgressChanged += (s, e) => { Console.Write("."); };
                    client.DownloadFile(packageUrl, downloadFileInfo.FullName);
                    downloadedNew = true;
                }
            }

            downloadFileInfo.Refresh();
            if (!downloadFileInfo.Exists)
                throw new IOException($"Unable to download the server from {packageUrl}");
            if (downloadFileInfo.Length != expectedSize)
                throw new IOException(
                    $"File at {packageUrl} was downloaded, but it's size {expectedSize.BytesToMegabytes()}Mb doesn't match the size expected {expectedSize.BytesToMegabytes()}Mb");

            Neo4jDir.Refresh();

            var extractedFolder = fileExtractor.ExtractFile(downloadFileInfo.FullName, Neo4jDir.FullName, downloadedNew);

            return new DirectoryInfo(Path.Combine(Neo4jDir.FullName, extractedFolder));
        }

        private static object GetPlatformFileExtension(Neo4jServerPlatform platform)
        {
            switch (platform)
            {
                case Neo4jServerPlatform.Windows:
                    return "zip";

                case Neo4jServerPlatform.Unix:
                    return "tar.gz";

                default:
                    throw new NotImplementedException();
            }
        }

        private static void EnsureDirectoriesExist()
        {
            if (!Neo4jDir.Exists)
                Neo4jDir.Create();
        }
    }
}
