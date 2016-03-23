namespace Neo4j.Driver.IntegrationTests.Internals
{
  /// <summary>
  /// File extractor
  /// </summary>
  internal interface INeo4jServerFileExtractor
  {
    /// <summary>
    /// Extract the given file.
    /// </summary>
    /// <param name="fileToExtractPath">The file to extract.</param>
    /// <param name="targetFolder">Target folder to extract to.</param>
    /// <param name="fileWasNewlyDownloaded">This is true if the file was just downloaded. Else false.</param>
    /// <returns>The final extracted folder, that is the base folder for the Neo4j server.</returns>
    string ExtractFile(string fileToExtractPath, string targetFolder, bool fileWasNewlyDownloaded);
  }
}
