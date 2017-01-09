using System.Collections.Generic;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public interface IInstaller
    {
        void Install();
        ISet<ISingleInstance> Start();
        void Stop();
        void Kill();
    }
}