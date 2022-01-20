using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend
{
	internal class ResultConsume : IProtocolObject
	{
		public ResultConsumeType data { get; set; } = new ResultConsumeType();
		[JsonIgnore]
		public IRecord Records { get; set; }
		[JsonIgnore]
		public IResultSummary Summary { get; set; }

		public class ResultConsumeType
		{
			public string resultId { get; set; }
		}

		public override async Task Process()
		{
			Summary = await ((Result)ObjManager.GetObject(data.resultId)).ConsumeResults().ConfigureAwait(false);
		}

		public override string Respond()
        {
            var queryType = Summary?.QueryType switch
            {
                QueryType.ReadOnly => "r",
                QueryType.ReadWrite => "rw",
                QueryType.WriteOnly => "w",
                QueryType.SchemaWrite => "s",
                QueryType.Unknown => null,
                _ => throw new ArgumentOutOfRangeException()
            };

            var response = new ProtocolResponse("Summary", new
            {
                query = Summary?.Query == null
                    ? null
                    : new
                    {
                        text = Summary.Query.Text,
                        parameters = Summary.Query.Parameters
                            .Select(x => new { x.Key, Value = NativeToCypher.Convert(x.Value) })
                            .ToDictionary(x => x.Key, x => x.Value)
                    },
                queryType = queryType,
                plan = Summary?.Plan == null 
                    ? null
                    : MapToPlanJson(Summary.Plan),
                notifications = CreateNotificationList(),
                database = Summary.Database?.Name,
                resultAvailableAfter = Summary?.ResultAvailableAfter.TotalMilliseconds >= 0L
                        ? Summary?.ResultAvailableAfter.TotalMilliseconds
                        : default(long?),
                resultConsumedAfter = Summary?.ResultConsumedAfter.TotalMilliseconds >= 0L
                    ? Summary?.ResultConsumedAfter.TotalMilliseconds
                    : default(long?),
                serverInfo = Summary?.Server == null
                    ? null
                    : new
                    {
                        protocolVersion = Summary.Server.ProtocolVersion,
                        agent = Summary.Server.Agent

                    },
                counters = new
                {
                    constraintsAdded = Summary.Counters.ConstraintsAdded,
                    constraintsRemoved = Summary.Counters.ConstraintsRemoved,
                    nodesCreated = Summary.Counters.NodesCreated,
                    nodesDeleted = Summary.Counters.NodesDeleted,
                    relationshipsCreated = Summary.Counters.RelationshipsCreated,
                    relationshipsDeleted = Summary.Counters.RelationshipsDeleted,
                    propertiesSet = Summary.Counters.PropertiesSet,
                    labelsAdded = Summary.Counters.LabelsAdded,
                    labelsRemoved = Summary.Counters.LabelsRemoved,
                    indexesAdded = Summary.Counters.IndexesAdded,
                    indexesRemoved = Summary.Counters.IndexesRemoved,
                    systemUpdates = Summary.Counters.SystemUpdates,
                    containsUpdates = Summary.Counters.ContainsUpdates,
                    containsSystemUpdates = Summary.Counters.ContainsSystemUpdates,
                },
                profile = MapToProfilePlan(Summary.Profile)
            });
                
            return response.Encode();
		}

        private object MapToProfilePlan(IProfiledPlan plan)
        {
            if (plan == null)
                return null;
            if (plan.HasPageCacheStats)
                return new
                {
                    args = plan.Arguments,
                    operatorType = plan.OperatorType,
                    children = plan.Children.Select(MapToProfilePlan).ToList(),
                    identifiers = plan.Identifiers,
                    time = plan.Time,
                    pageCacheHitRatio = plan.PageCacheHitRatio,
                    pageCacheMisses = plan.PageCacheMisses,
                    pageCacheHits = plan.PageCacheHits,
                    rows = plan.Records,
                    dbHits = plan.DbHits,
                };

            return new
            {
                args = plan.Arguments,
                operatorType = plan.OperatorType,
                children = plan.Children.Select(MapToProfilePlan).ToList(),
                identifiers = plan.Identifiers,
                rows = plan.Records,
                dbHits = plan.DbHits,
            };
        }

        private object MapToPlanJson(IPlan plan)
        {
            return new
            {
                args = plan.Arguments,
                operatorType = plan.OperatorType,
                children = plan.Children.Select(MapToPlanJson).ToList(),
                identifiers = plan.Identifiers
            };
        }

        private object CreateNotificationList()
        {
            if (Summary?.Notifications == null)
                return null;
            if (Summary?.Notifications?.All(x => x.Position == null) ?? false)
            {
                return Summary?.Notifications.Select(x => new
                {
                    severity = x.Severity,
                    description = x.Description,
                    code = x.Code,
                    title = x.Title,
                }).ToList();
            }

            return Summary?.Notifications.Select(x => new
            {
                severity = x.Severity,
                description = x.Description,
                code = x.Code,
                title = x.Title,
                position = x.Position == null 
                    ? null
                    : new
                    {
                        column = x.Position.Column,
                        offset = x.Position.Offset,
                        line = x.Position.Line
                    }
            }).ToList();
        }
    }
}
