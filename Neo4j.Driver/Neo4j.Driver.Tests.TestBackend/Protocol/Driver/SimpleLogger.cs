using System;

namespace Neo4j.Driver.Tests.TestBackend
{
    internal class SimpleLogger : ILogger
    {
        public void Debug(string message, params Object[] args)
        {
            Console.WriteLine($"[DRIVER-DEBUG][{Now}]{message}", args);
        }
        public void Error(System.Exception error, string message, params Object[] args)
        {
            Console.WriteLine($"[DRIVER-ERROR][{Now}]{message}", args);
        }
        public void Info(string message, params Object[] args)
        {
            Console.WriteLine($"[DRIVER-INFO] [{Now}]{message}", args);
        }
        public bool IsDebugEnabled()
        {
            return true;
        }
        public bool IsTraceEnabled()
        {
            return true;
        }
        public void Trace(string message, params Object[] args)
        {
            Console.WriteLine($"[DRIVER-TRACE][{Now}]{message}", args);
        }
        public void Warn(System.Exception error, string message, params Object[] args)
        {
            Console.WriteLine($"[DRIVER-WARN] [{Now}]{message}", args);
        }
        private string Now => DateTime.UtcNow.ToString("O");
    }
}