using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver.Extensions;

namespace Neo4j.Driver
{
    public enum LogLevel
    {
        None,
        Error,
        Info,
        Debug,
        Trace
    }

    public interface ILogger : IDisposable
    {
        void Error(string message, Exception cause = null, params object[] restOfMessage);

        /// <summary>Log a message at info level.</summary>
        /// <param name="message">The message.</param>
        /// <param name="restOfMessage">Any restOfMessage parts of the message, including (but not limited to) Sent Messages and Received Messages.</param>
        void Info(string message, params object[] restOfMessage);

        void Debug(string message, params object[] restOfMessage);

        void Trace(string message, params object[] restOfMessage);

        LogLevel Level { get; set; }
    }

    public abstract class BaseOutLogger : ILogger
    {
        protected Action<string> LogMethod { get; set; }
        public LogLevel Level { get; set; }

        protected BaseOutLogger() { }
        protected BaseOutLogger(Action<string> logMethod)
        {
            LogMethod = logMethod;
        }

        private void Log(LogLevel level, string message, params object[] restOfMessage)
        {
            if (Level < level || level == LogLevel.None)
            {
                return;
            }
            
           LogMethod($"[{level}] => {message}{ObjectToString(restOfMessage)}");
        }

        private string ObjectToString(object o)
        {
            if (o == null)
            {
                return "NULL";
            }
            if (o is string)
            {
                return o.ToString();
            }
            if (o is byte[])
            {
                return ((byte[])o).ToHexString();
            }

            if (o is IEnumerable)
            {
                return EnumerableToString(o as IEnumerable);
            }

            return o.ToString();
        }

        private string EnumerableToString(IEnumerable items)
        {
            var output = (from object item in items select ObjectToString(item)).ToList();
            return $"[{string.Join(", ", output)}]";
        }


        // ALL, SEVERE, INFO, DEBUG, 
        public void Error(string message, Exception cause = null, params object[] restOfMessage)
        {
            Log(LogLevel.Error, message, cause, restOfMessage);
        }

        public void Info(string message, params object[] restOfMessage)
        {
            Log(LogLevel.Info, message, restOfMessage);
        }

        public void Debug(string message, params object[] restOfMessage)
        {
            Log(LogLevel.Debug, message, restOfMessage);
        }

        public void Trace(string message, params object[] restOfMessage)
        {
            Log(LogLevel.Trace, message, restOfMessage);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            //Do nothing
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class DebugLogger : BaseOutLogger
    {
        public DebugLogger() :
            base(s => System.Diagnostics.Debug.WriteLine(s))
        { }
    }
}
