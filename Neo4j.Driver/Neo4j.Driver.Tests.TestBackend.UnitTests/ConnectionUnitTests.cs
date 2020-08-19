using Xunit;

namespace Neo4j.Driver.Tests.TestBackend.UnitTests
{
    public class ConnectionUnitTests
    {
        [Fact]
        public void ShouldOpenAndConnectWithoutError()
        {
            /* TODO... use this rather than external client. Needs to be got working in a better way though.
            const string args = "..\\..\\..\\..\\DotNetNutkit\\bin\\Debug\\netcoreapp3.1\\NutKitDotNet.exe 9001";

            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = false;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = "/C " + args;
            cmd.Start();

            //cmd.StandardInput.WriteLine("echo Oscar");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            //cmd.WaitForExit();

            string standardOutput = cmd.StandardOutput.ReadToEnd();
            Debug.WriteLine(standardOutput);
            */
        }
    }
}