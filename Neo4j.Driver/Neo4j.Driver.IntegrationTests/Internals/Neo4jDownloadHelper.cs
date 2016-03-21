using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.IntegrationTests.Internals
{
  class Neo4jDownloadHelper
  {
    private static string Version => Environment.GetEnvironmentVariable("version") ?? "3.0.0-NIGHTLY";
    //        private static string PackageUrl => $"http://alpha.neohq.net/dist/neo4j-enterprise-{Version}-windows.zip";
    private static string PackageUrl => $"http://alpha.neohq.net/dist/neo4j-community-{Version}-windows.zip";

    private static DirectoryInfo Neo4jDir => new DirectoryInfo("../target/neo4j");

    public static DirectoryInfo DownloadNeo4j()
    {
      EnsureDirectoriesExist();

      var downloadFileInfo = new FileInfo($"../target/{Version}.zip");
      if (downloadFileInfo.Directory != null)
      {
        if (!downloadFileInfo.Directory.Exists)
          downloadFileInfo.Directory.Create();
      }

      bool downloadedNew = false;
      long expectedSize;
      using (var client = new WebClient())
      {
        client.OpenRead(PackageUrl);
        expectedSize = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
        if (!downloadFileInfo.Exists || downloadFileInfo.Length != expectedSize)
        {
          client.DownloadProgressChanged += (s, e) => { Console.Write("."); };
          client.DownloadFile(PackageUrl, downloadFileInfo.FullName);
          downloadedNew = true;
        }
      }

      downloadFileInfo.Refresh();
      if (!downloadFileInfo.Exists)
        throw new IOException($"Unable to download the server from {PackageUrl}");
      if (downloadFileInfo.Length != expectedSize)
        throw new IOException($"File at {PackageUrl} was downloaded, but it's size {expectedSize.BytesToMegabytes()}Mb doesn't match the size expected {expectedSize.BytesToMegabytes()}Mb");

      Neo4jDir.Refresh();
      var zipFolder = GetZipFolder(downloadFileInfo.FullName);

      var extractedPath = Path.Combine(Neo4jDir.FullName, zipFolder);

      if (Directory.Exists(extractedPath))
      {
        var dirs = Neo4jDir.GetDirectories(zipFolder);
        if (dirs.Length != 0)
        {
          var extractedDir = new DirectoryInfo(Path.Combine(Neo4jDir.FullName, zipFolder));
          if (downloadedNew)
          {
            extractedDir.Delete(true);
            ExtractZip(downloadFileInfo.FullName);
          }
        }
      }
      else
      {
        ExtractZip(downloadFileInfo.FullName);
      }

      return new DirectoryInfo(Path.Combine(Neo4jDir.FullName, zipFolder));
    }

    private static void EnsureDirectoriesExist()
    {
      if (!Neo4jDir.Exists)
        Neo4jDir.Create();
    }

    private static string GetZipFolder(string filename)
    {
      using (var archive = ZipFile.OpenRead(filename))
      {
        return archive.Entries.Where(a => a.Name == string.Empty).OrderBy(a => a.FullName.Length).First().FullName;
      }
    }

    private static void ExtractZip(string filename)
    {
      ZipFile.ExtractToDirectory(filename, Neo4jDir.FullName);
    }
  }
}
