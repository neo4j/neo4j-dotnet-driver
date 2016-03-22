using Neo4j.Driver.IntegrationTests.Internals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.IntegrationTests
{
  /// <summary>
  /// A process based Neo4j installer that uses a background process for 
  /// running the Neo4j server. 
  /// </summary>
  public class ProcessBasedNeo4jInstaller : INeo4jInstaller
  {
    private const string eventSourceName = "Neo4jTests";

    public DirectoryInfo Neo4jHome { get; private set; }

    public void DownloadNeo4j()
    {
      Neo4jHome = Neo4jDownloadHelper.DownloadNeo4j();

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
        processInfo.FileName = @"java.exe";
        processInfo.WorkingDirectory = Neo4jHome.FullName;
        processInfo.Arguments = string.Format(@"-cp ""{0}lib/*;{0}plugins/*"" -server -Dorg.neo4j.config.file=conf/neo4j.conf -Dlog4j.configuration=file:conf/log4j.properties -Dneo4j.ext.udc.source=zip-powershell -Dorg.neo4j.cluster.logdirectory=data/log -Dorg.neo4j.config.file=conf/neo4j.conf -XX:+UseG1GC -XX:-OmitStackTraceInFastThrow -XX:hashCode=5 -XX:+AlwaysPreTouch -XX:+UnlockExperimentalVMOptions -XX:+TrustFinalNonStaticFields -XX:+DisableExplicitGC -Dunsupported.dbms.udc.source=zip -Dfile.encoding=UTF-8 org.neo4j.server.CommunityEntryPoint", Neo4jHome.FullName);
        processInfo.UseShellExecute = false;
        processInfo.CreateNoWindow = true;
        processInfo.RedirectStandardInput = true;
        processInfo.RedirectStandardOutput = true;

        EventLog.WriteEntry(eventSourceName, $"Starting process: {processInfo.FileName} with working directory: {processInfo.WorkingDirectory} and arguments: {processInfo.Arguments}", EventLogEntryType.Information);

        startedProcess = Process.Start(processInfo);
      }
      catch (Exception ex)
      {
        EventLog.WriteEntry(eventSourceName, ex.ToString(), EventLogEntryType.Error);
        throw;
      }

      Task task = new Task(() =>
      {
        StringBuilder logCollectlor = new StringBuilder();
        while (!startedProcess.HasExited)
        {
          string line = startedProcess.StandardOutput.ReadLine();
          logCollectlor.AppendLine(line);
        }

        string log = logCollectlor.ToString();
        if (log.Contains("ERROR"))
        {
          EventLog.WriteEntry(eventSourceName, log, EventLogEntryType.Error);

          // This exception is only seen when debugging unittest
          // This can be helpful debugging if/why the Neo4j server does not start/work
          throw new InvalidOperationException(log);
        }
        else
        {
          EventLog.WriteEntry(eventSourceName, log, EventLogEntryType.Information);
        }
      });
      task.Start();

      Task.Delay(15000).Wait();
    }

    public void StopServer()
    {
      if (startedProcess == null) return;

      try
      {
        EventLog.WriteEntry(eventSourceName, "Stopping process");
        startedProcess.StandardInput.AutoFlush = true;
        startedProcess.StandardInput.Close();
        startedProcess.CloseMainWindow();
        Task.Delay(10000).Wait();
        startedProcess.Kill();
        startedProcess.WaitForExit();
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
