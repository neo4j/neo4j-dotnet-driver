using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Neo4j.Driver.IntegrationTests.Internals
{
  /// <summary>
  /// A process based Neo4j installer that uses a background process for 
  /// running the Neo4j server. 
  /// </summary>
  public class WindowsProcessBasedNeo4jInstaller : INeo4jInstaller
  {
    private const string eventSourceName = "Neo4jTests";

    private readonly int delayAfterStartingServer;    

    public WindowsProcessBasedNeo4jInstaller(int delayAfterStartingServer = 15000)
    {
      this.delayAfterStartingServer = delayAfterStartingServer;
    }

    public DirectoryInfo Neo4jHome { get; private set; }

    public void DownloadNeo4j()
    {
      Neo4jHome = Neo4jServerFilesDownloadHelper.DownloadNeo4j(
        Neo4jServerEdition.Enterprise, 
        Neo4jServerPlatform.Windows, 
        new ZipNeo4jServerFileExtractor());

      UpdateSettings(new Dictionary<string, string> { { "dbms.security.auth_enabled", "false" } });// disable auth
    }

    public void InstallServer()
    {
      // Not needed
    }

    private Process startedProcess = null;
    public void StartServer()
    {
      try
      {
        ProcessStartInfo processInfo = new ProcessStartInfo();
        processInfo.FileName = Path.Combine(Neo4jHome.FullName, @"bin\neo4j.bat");
        processInfo.WorkingDirectory = Neo4jHome.FullName;
        processInfo.Arguments = "console";
        processInfo.UseShellExecute = true;
        processInfo.CreateNoWindow = false;

        EventLog.WriteEntry(eventSourceName, $"Starting process: {processInfo.FileName} with working directory: {processInfo.WorkingDirectory} and arguments: {processInfo.Arguments}", EventLogEntryType.Information);

        startedProcess = Process.Start(processInfo);
      }
      catch (Exception ex)
      {
        EventLog.WriteEntry(eventSourceName, ex.ToString(), EventLogEntryType.Error);
        throw;
      }

      Task.Delay(delayAfterStartingServer).Wait();
    }

    public void StopServer()
    {
      if (startedProcess == null) return;

      try
      {
        EventLog.WriteEntry(eventSourceName, "Stopping process");
        startedProcess.CloseMainWindow();
        if (!startedProcess.WaitForExit(10000))
        {
          startedProcess.Kill();
          startedProcess.WaitForExit();
        }
        startedProcess = null;
      }
      catch (Exception ex)
      {
        EventLog.WriteEntry(eventSourceName, ex.ToString(), EventLogEntryType.Error);
        throw;
      }
    }

    public void UninstallServer()
    {
      // Not needed
    }

    public void UpdateSettings(IDictionary<string, string> keyValuePair)
    {
      Neo4jSettingsHelper.UpdateSettings(Neo4jHome.FullName, keyValuePair);
    }
  }
}
