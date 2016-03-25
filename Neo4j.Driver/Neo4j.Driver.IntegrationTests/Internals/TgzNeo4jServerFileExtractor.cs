using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    /// <summary>
    ///     Tgz (tar.gz) implementation of the <see cref="INeo4jServerFileExtractor"/>.
    /// </summary>
    internal class TgzNeo4jServerFileExtractor : INeo4jServerFileExtractor
    {
        public string ExtractFile(string fileToExtractPath, string targetFolder, bool fileWasNewlyDownloaded)
        {
            var destFolderName = GetDestFolderName(fileToExtractPath);
            var destinationFolderPath = Path.Combine(targetFolder, destFolderName);

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

            FixPermissions(Path.Combine(targetFolder, destFolderName, "bin/neo4j"));

            return destFolderName;
        }

        private static string GetDestFolderName(string filename)
        {
            using (Stream inStream = File.OpenRead(filename))
            using (Stream gzipStream = new GZipInputStream(inStream))
            using (TarInputStream tarIn = new TarInputStream(gzipStream))
            {
                TarEntry tarEntry;
                while ((tarEntry = tarIn.GetNextEntry()) != null)
                {
                    if (tarEntry.IsDirectory && tarEntry.Name.Count(f => f == '/') == 1)
                    {
                        return tarEntry.Name.Substring(0, tarEntry.Name.Length - 1);
                    }
                }

                throw new InvalidOperationException("Root folder in tar not found");
            }
        }

        private static void ExtractZip(string filename, string destinationFolder)
        {
            using (Stream inStream = File.OpenRead(filename))
            using (Stream gzipStream = new GZipInputStream(inStream))
            using (TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream))
            {
                tarArchive.ExtractContents(destinationFolder);
            }
        }

        private static void FixPermissions(string filename)
        {
            File.AppendAllLines("log.txt", new[] { filename });

            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = "chmod";
            processInfo.Arguments = "+x " + filename;

            Process process = Process.Start(processInfo);
            process.WaitForExit();

            File.AppendAllLines("log.txt", new[] { "chmod done!" });
        }
    }
}

