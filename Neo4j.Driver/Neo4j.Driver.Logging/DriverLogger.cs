using System;
using Microsoft.Extensions.Logging;
using Neo4j.Driver.V1;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Neo4j.Driver.Extensions.Logging
{
    public class DriverLogger : IDriverLogger
    {
        private readonly ILogger _delegator;

        public DriverLogger(ILogger delegator)
        {
            _delegator = delegator;
        }

        public void Error(Exception cause, string message, params object[] args)
        {
            _delegator.LogError(default(EventId), cause, message, args);
        }

        public void Warn(Exception cause, string message, params object[] args)
        {
            _delegator.LogWarning(default(EventId), cause, message, args);
        }

        public void Info(string message, params object[] args)
        {
            _delegator.LogInformation(default(EventId), message, args);
        }

        public void Debug(string message, params object[] args)
        {
            // No need to test if debug is enabled or not as we shall not come to here if it is not enabled.
            _delegator.LogDebug(default(EventId), message, args);
        }

        public void Trace(string message, params object[] args)
        {
            // No need to test if trace is enabled or not as we shall not come to here if it is not enabled.
            _delegator.LogTrace(default(EventId), message, args);
        }

        public bool IsTraceEnabled()
        {
            return _delegator.IsEnabled(LogLevel.Trace);
        }

        public bool IsDebugEnabled()
        {
            return _delegator.IsEnabled(LogLevel.Debug);
        }
    }
}