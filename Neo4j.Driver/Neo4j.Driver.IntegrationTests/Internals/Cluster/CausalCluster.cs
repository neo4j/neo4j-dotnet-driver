using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.IntegrationTests.Internals
{
    public class CausalCluster : IDisposable
    {
        private readonly ExternalPythonClusterInstaller _installer = new ExternalPythonClusterInstaller();
        public ISet<ISingleInstance> ClusterMembers { get; }

        // Assume the whole cluster use exact the same authToken
        public IAuthToken AuthToken => ClusterMembers?.First().AuthToken;

        public CausalCluster()
        {
            // Do not start a server if boltkit is not available locally.
            if (!_installer.IsBoltkitAvaliable())
            {
                return;
            }
            // start a cluster
            try
            {
                _installer.Install();
                ClusterMembers = _installer.Start();
                foreach (var singleInstance in ClusterMembers)
                {
                    Console.WriteLine(singleInstance);
                }
            }
            catch
            {
                try
                {
                    Kill();
                }
                catch
                {
                    // do nothing
                }
                throw;
            }
        }

        public ISingleInstance AnyCore()
        {
            return ClusterMembers.First();
        }

        public bool IsClusterRunning()
        {
            return ClusterMembers != null;
        }

        private void Kill()
        {
            // Unlike Dispose, this method will always try to execute
            try
            {
                _installer.Kill();
            }
            catch
            {
                // ignored
            }
        }

        public void Dispose()
        {
            // There is nothing to dispose if we did not managed to start any cluster at all.
            if (!IsClusterRunning())
            {
                // failed to init successfully
                return;
            }

            // shut down the whole cluster
            try
            {
                _installer.Stop();
            }
            catch
            {
                // if failed to stop properly, then we kill
                try
                {
                    Kill();
                }
                catch
                {
                    // ignored
                }
                // ignored
            }
        }
    }
}
