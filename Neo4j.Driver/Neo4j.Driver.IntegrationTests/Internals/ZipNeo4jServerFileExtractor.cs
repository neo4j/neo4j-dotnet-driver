using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Neo4j.Driver.IntegrationTests.Internals
{
  /// <summary>
  /// ZIP based implementation of <see cref="INeo4jServerFileExtractor"/>.
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
        return archive.Entries.Where(a => a.Name == string.Empty).OrderBy(a => a.FullName.Length).First().FullName;
      }
    }

    private static void ExtractZip(string filename, string destinationFolder)
    {
      ZipFile.ExtractToDirectory(filename, destinationFolder);
    }
  }
}
