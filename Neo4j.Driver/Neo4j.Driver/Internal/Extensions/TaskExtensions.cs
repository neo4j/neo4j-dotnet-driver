using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver.Internal
{
    internal static class TaskExtensions
    {
#if NET452
        private static readonly Task completedTask = Task.WhenAll();
#endif

        public static Task GetCompletedTask()
        {
#if NET452
            return completedTask;
#else
            return Task.CompletedTask;
#endif
        }

    }
}
