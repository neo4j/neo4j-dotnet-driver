using System;

namespace Neo4j.Driver.Tests.TestBackend;

internal class SimpleLogger : ILogger
{
    private string Now => DateTime.UtcNow.ToString("O");

    public void Debug(string message, params object[] args)
    {
        Console.WriteLine($"[DRIVER-DEBUG][{Now}]{message}", args);
    }

    public void Error(Exception error, string message, params object[] args)
    {
        Console.WriteLine($"[DRIVER-ERROR][{Now}]{message}", args);
    }

    public void Info(string message, params object[] args)
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

    public void Trace(string message, params object[] args)
    {
        Console.WriteLine($"[DRIVER-TRACE][{Now}]{message}", args);
    }

    public void Warn(Exception error, string message, params object[] args)
    {
        Console.WriteLine($"[DRIVER-WARN] [{Now}]{message}", args);
    }
}
