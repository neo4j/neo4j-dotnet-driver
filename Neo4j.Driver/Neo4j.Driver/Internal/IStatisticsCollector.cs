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
        // Pull statistics from the statistics providers.
        // The statistics will only be updated when this method is called.
        IDictionary<string, object> CollectStatistics();
        // Clear all registered providers so that this provider could be used by other drivers if needed.
        void Clear();
    }

    internal interface IStatisticsProvider
    {
        IDictionary<string, object> ReportStatistics();
    }

    internal class StatisticsCollector : IStatisticsCollector
    {
        private readonly IList<IStatisticsProvider> _providers = new List<IStatisticsProvider>();

        public void Register(IStatisticsProvider provider)
        {
            _providers.Add(provider);
        }

        public IDictionary<string, object> CollectStatistics()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            foreach (var provider in _providers)
            {
                foreach (var statistic in provider.ReportStatistics())
                {
                    dict.Add(statistic);
                }
            }
            return dict;
        }

        public void Clear()
        {
            _providers.Clear();
        }
    }
}
