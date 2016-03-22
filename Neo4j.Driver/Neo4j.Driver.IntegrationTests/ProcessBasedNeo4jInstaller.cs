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
      // @"C:\Program Files\Java\jre1.8.0_73\bin\java.exe - cp ""C:\Source\neo4j\ingvar\neo4j-dotnet-driver\Neo4j.Driver\Neo4j.Driver.IntegrationTests\bin\target\neo4j\neo4j-community-3.0.0-RC1/lib/*;C:\Source\neo4j\ingvar\neo4j - dotnet - driver\Neo4j.Driver\Neo4j.Driver.IntegrationTests\bin\target\neo4j\neo4j - community - 3.0.0 - RC1 / plugins/*"" -server -Dorg.neo4j.config.file=conf/neo4j.conf -Dlog4j.configuration=file:conf/log4j.properties -Dneo4j.ext.udc.source=zip-powershell -Dorg.neo4j.cluster.logdirectory=data/log -Dorg.neo4j.config.file=conf/neo4j.conf -XX:+UseG1GC -XX:-OmitStackTraceInFastThrow -XX:hashCode=5 -XX:+AlwaysPreTouch -XX:+UnlockExperimentalVMOptions -XX:+TrustFinalNonStaticFields -XX:+DisableExplicitGC -Dunsupported.dbms.udc.source=zip -Dfile.encoding=UTF-8 org.neo4j.server.CommunityEntryPoint"
      ProcessStartInfo processInfo = new ProcessStartInfo();
      processInfo.FileName = @"C:\Program Files\Java\jre1.8.0_73\bin\java.exe";
      processInfo.WorkingDirectory = @"C:\Source\neo4j\ingvar\neo4j-dotnet-driver\Neo4j.Driver\Neo4j.Driver.IntegrationTests\bin\target\neo4j\neo4j-community-3.0.0-RC1";
      processInfo.Arguments = @"-cp ""C:\Source\neo4j\ingvar\neo4j-dotnet-driver\Neo4j.Driver\Neo4j.Driver.IntegrationTests\bin\target\neo4j\neo4j-community-3.0.0-RC1/lib/*;C:\Source\neo4j\ingvar\neo4j - dotnet - driver\Neo4j.Driver\Neo4j.Driver.IntegrationTests\bin\target\neo4j\neo4j - community - 3.0.0 - RC1 / plugins/*"" -server -Dorg.neo4j.config.file=conf/neo4j.conf -Dlog4j.configuration=file:conf/log4j.properties -Dneo4j.ext.udc.source=zip-powershell -Dorg.neo4j.cluster.logdirectory=data/log -Dorg.neo4j.config.file=conf/neo4j.conf -XX:+UseG1GC -XX:-OmitStackTraceInFastThrow -XX:hashCode=5 -XX:+AlwaysPreTouch -XX:+UnlockExperimentalVMOptions -XX:+TrustFinalNonStaticFields -XX:+DisableExplicitGC -Dunsupported.dbms.udc.source=zip -Dfile.encoding=UTF-8 org.neo4j.server.CommunityEntryPoint";
      processInfo.UseShellExecute = false;
      processInfo.RedirectStandardInput = true;
      processInfo.RedirectStandardOutput = true;
      startedProcess = Process.Start(processInfo);

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
          // This exception is only seen when debugging unittest
          // This can be helpful debugging if/why the Neo4j server does not start/work
          throw new InvalidOperationException(log);
        }
      });
      task.Start();

      Task.Delay(15000).Wait();
    }

    public void StopServer()
    {
      startedProcess.StandardInput.Close();
      Task.Delay(5000).Wait();
      startedProcess.Kill();
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
