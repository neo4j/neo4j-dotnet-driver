// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
//
// This file is part of Neo4j.
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
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
using System.Diagnostics;
using System.Linq;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.Telemetry;

internal class TelemetryManager
{
    private readonly IActivityProvider _activityProvider;

    private class ApiActivity : IDisposable
    {
        public Activity Activity { get; }

        public ApiActivity(QueryApiType queryApiType, IActivityProvider activityProvider)
        {
            Activity = activityProvider.CreateActivity($"api:{queryApiType.ToString()}", ActivityKind.Internal);
            Activity.SetTag("queryApiType", queryApiType.ToString());
            Activity.Start();
        }

        public void Dispose()
        {
            Activity?.Stop();
        }
    }

    public TelemetryManager(
        ITelemetryCollector telemetryCollector = null,
        IActivityProvider activityProvider = null)
    {
        telemetryCollector ??= TelemetryCollector.Default;
        _activityProvider = activityProvider ?? ActivityProvider.Default;

        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity =>
            {
                if (!IsOutermostApiActivity(activity))
                {
                    return;
                }

                // tell the telemetry collector that we have started an api activity
                var apiType = activity.Tags.FirstOrDefault(x => x.Key == "queryApiType").Value ?? "unknown";

                if (Enum.TryParse(apiType, out QueryApiType queryApiType))
                {
                    telemetryCollector.SetQueryApiType(queryApiType);
                }
            },
        };

        ActivitySource.AddActivityListener(listener);
    }

    public IDisposable StartApiActivity(QueryApiType queryApiType)
    {
        return new ApiActivity(queryApiType, _activityProvider);
    }

    private static bool IsOutermostApiActivity(Activity activity)
    {
        if (!activity.OperationName.StartsWith("api:"))
        {
            // if it's not an api activity we can just return false
            return false;
        }

        // go up the tree until we find the outermost activity that starts with "api:"
        var currentActivity = activity.Parent;
        while (currentActivity != null)
        {
            if (currentActivity.OperationName.StartsWith("api:"))
            {
                // we found an ancestor that is an api activity
                return false;
            }

            currentActivity = currentActivity.Parent;
        }

        // we didn't find any ancestor that is an api activity, so this is the outermost one
        return true;
    }
}
