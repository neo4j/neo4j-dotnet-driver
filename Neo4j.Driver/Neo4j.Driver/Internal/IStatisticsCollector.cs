using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j.Driver
{
    internal interface IStatisticsCollector
    {
        // Register a provider where the collector could pull statistics.
        // This method will be used internally by the driver to plug in statistics providers
        void Register(IStatisticsProvider provider);
        // Unregister a provider from the collector
        // This method will be used internally by the driver to remove statistics providers when the provider is no longer valid.
        // The provider will not immediately be removed from collector but removed after next CollectStatistics call.
        bool Unregister(IStatisticsProvider provider);
        // Pull statistics from the statistics providers.
        // The statistics will only be updated when this method is called.
        IDictionary<string, object> CollectStatistics();
        // Clear all providers so that this collector could be used by other drivers if needed.
        void Clear();
    }

    internal interface IStatisticsProvider
    {
        String GetUniqueName();
        IDictionary<string, object> ReportStatistics();
    }

    internal class StatisticsCollector : IStatisticsCollector
    {
        private readonly IList<IStatisticsProvider> _providers = new List<IStatisticsProvider>();
        private readonly IList<IStatisticsProvider> _removed = new List<IStatisticsProvider>();

        public void Register(IStatisticsProvider provider)
        {
            _providers.Add(provider);
        }

        public bool Unregister(IStatisticsProvider provider)
        {
            if (_providers.Remove(provider))
            {
                _removed.Add(provider);
                return true;
            }
            return false;
        }

        public IDictionary<string, object> CollectStatistics()
        {
            IDictionary<string, object> statDict = new Dictionary<string, object>();
            var providerSnapshot = _providers.ToArray();
            var removedSnapshot = _removed.ToArray();
            CollectStatistics(statDict, providerSnapshot);
            CollectStatistics(statDict, removedSnapshot);
            // only remove after the removed already be collected
            UnregisterRemoved(removedSnapshot);
            return statDict;
        }

        private void UnregisterRemoved(IStatisticsProvider[] removed)
        {
            foreach (var provider in removed)
            {
                _removed.Remove(provider);
            }
        }

        private static void CollectStatistics(IDictionary<string, object> statDict, IStatisticsProvider[] providers)
        {
            foreach (var provider in providers)
            {
                IDictionary<string, object> dict = new Dictionary<string, object>();
                statDict.Add(provider.GetUniqueName(), dict);
                foreach (var statistic in provider.ReportStatistics())
                {
                    dict.Add(statistic);
                }
            }
        }

        public void Clear()
        {
            _providers.Clear();
            _removed.Clear();
        }
    }
}
