// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal.MessageHandling.V4
{
    internal class PullResponseHandler : MetadataCollectingResponseHandler
    {
        private readonly IResultStreamBuilder _streamBuilder;
        private readonly SummaryBuilder _summaryBuilder;
        private readonly IBookmarksTracker _bookmarksTracker;

        public PullResponseHandler(IResultStreamBuilder streamBuilder, SummaryBuilder summaryBuilder,
            IBookmarksTracker bookmarksTracker)
        {
            _streamBuilder = streamBuilder ?? throw new ArgumentNullException(nameof(streamBuilder));
            _summaryBuilder = summaryBuilder ?? throw new ArgumentNullException(nameof(summaryBuilder));
            _bookmarksTracker = bookmarksTracker;

            AddMetadata<BookmarksCollector, Bookmarks>();
            AddMetadata<HasMoreCollector, bool>();
            AddMetadata<TimeToLastCollector, long>();
            AddMetadata<TypeCollector, QueryType>();
            AddMetadata<CountersCollector, ICounters>();
            AddMetadata<PlanCollector, IPlan>();
            AddMetadata<ProfiledPlanCollector, IProfiledPlan>();
            AddMetadata<NotificationsCollector, IList<INotification>>();
            AddMetadata<DatabaseInfoCollector, IDatabaseInfo>();
        }

        public override void OnSuccess(IDictionary<string, object> metadata)
        {
            base.OnSuccess(metadata);

            _bookmarksTracker?.UpdateBookmarks(GetMetadata<BookmarksCollector, Bookmarks>(), GetMetadata<DatabaseInfoCollector, IDatabaseInfo>());

            _summaryBuilder.ResultConsumedAfter = GetMetadata<TimeToLastCollector, long>();
            _summaryBuilder.Counters = GetMetadata<CountersCollector, ICounters>();
            _summaryBuilder.Notifications = GetMetadata<NotificationsCollector, IList<INotification>>();
            _summaryBuilder.Plan = GetMetadata<PlanCollector, IPlan>();
            _summaryBuilder.Profile = GetMetadata<ProfiledPlanCollector, IProfiledPlan>();
            _summaryBuilder.QueryType = GetMetadata<TypeCollector, QueryType>();
            _summaryBuilder.Database = GetMetadata<DatabaseInfoCollector, IDatabaseInfo>();

            _streamBuilder.PullCompleted(GetMetadata<HasMoreCollector, bool>(), null);
        }

        public override void OnFailure(IResponsePipelineError error)
        {
            _streamBuilder.PullCompleted(false, error);
        }

        public override void OnIgnored()
        {
            _streamBuilder.PullCompleted(false, null);
        }

        public override void OnRecord(object[] fieldValues)
        {
            _streamBuilder.PushRecord(fieldValues);
        }
    }
}