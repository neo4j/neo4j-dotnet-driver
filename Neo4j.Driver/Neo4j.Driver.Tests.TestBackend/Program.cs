using System;
using System.Diagnostics;
using System.Threading.Tasks;


namespace Neo4j.Driver.Tests.TestBackend
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            ConsoleTraceListener consoleTraceListener = new ConsoleTraceListener();
            consoleTraceListener.Name = "Main output";
            Trace.Listeners.Add(consoleTraceListener);

            try
            {
                //TODO... arg error checking required.

                Trace.WriteLine($"Starting NutKitDotNet on {args[0]}:{args[1]}");
                var address = args[0];
                var port = Convert.ToInt32(args[1]);

                using var connection = new Connection(address, port);
                Controller controller = new Controller(connection);
                await controller.Process().ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                Trace.WriteLine(ex.Message);
                Trace.WriteLine($"Exception Details: \n {ex.StackTrace}");
            }
            finally
            {
                Trace.Flush();
                Trace.Listeners.Remove(consoleTraceListener);
                consoleTraceListener.Close();
                Trace.Close();
            }
        }
    }
}





