using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;



namespace Neo4j.Driver.Tests.TestBackend
{
    public class Program
    {
        private static IPAddress Address = null;
        private static uint Port = 0;
        
        static async Task Main(string[] args)
        {
            ConsoleTraceListener consoleTraceListener = new ConsoleTraceListener();
            consoleTraceListener.Name = "Main output";
            Trace.Listeners.Add(consoleTraceListener);

            try
            {

                ArgumentsValidation(args);

                using var connection = new Connection(Address.ToString(), Port);
                Controller controller = new Controller(connection);
                await controller.Process().ConfigureAwait(false);
            }
            catch(System.Exception ex)
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

        private static void ArgumentsValidation(string[] args)
        {
            if (args.Length < 2)
            {
                throw new IOException($"Incorrect number of arguments passed in. Expecting Address Port, but got {args.Length} arguments");
            }
            
            if(!uint.TryParse(args[1], out Port))
            {
                throw new IOException($"Invalid port passed in parameter 2.  Should be unsigned integer but was: {args[1]}.");
            }

            if(!IPAddress.TryParse(args[0], out Address))
            {
                throw new IOException($"Invalid IPAddress passed in parameter 1. {args[0]}");
            }

            if (args.Length > 2) {
                Trace.Listeners.Add(new TextWriterTraceListener(args[2]));
                Trace.WriteLine("Logging to file: " + args[2]);
            }

            Trace.WriteLine($"Starting TestBackend on {Address}:{Port}");
        }
    }
}





