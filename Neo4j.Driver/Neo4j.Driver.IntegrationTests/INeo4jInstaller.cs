using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Neo4j.Driver.IntegrationTests
{
  /// <summary>
  /// Neo4j installer
  /// </summary>
  public interface INeo4jInstaller
  {
    /// <summary>
    /// The Neo4j binaries folder.
    /// </summary>
    /// <remarks>
    /// This only defined if <see cref="DownloadNeo4j"/> has been called.
    /// </remarks>
    DirectoryInfo Neo4jHome { get; }

    /// <summary>
    /// Downloads the Neo4j binaries
    /// </summary>
    /// <returns></returns>
    void DownloadNeo4j();

    /// <summary>
    /// Installs Neo4j server as a service (Windows only)
    /// </summary>
    void InstallServer();

    /// <summary>
    /// Starts the Neo4j server (Any platform)
    /// </summary>
    void StartServer();

    /// <summary>
    /// Tops the Neo4j server (ANy platform)
    /// </summary>
    void StopServer();

    /// <summary>
    /// Uninstalls the Neo4j server (Windows only) <see cref="InstallServer"/>
    /// </summary>
    void UninstallServer();

    /// <summary>
    /// Updates the Neo4j server settings
    /// </summary>
    /// <param name="keyValuePair"></param>
    void UpdateSettings(IDictionary<string, string> keyValuePair);
  }
}