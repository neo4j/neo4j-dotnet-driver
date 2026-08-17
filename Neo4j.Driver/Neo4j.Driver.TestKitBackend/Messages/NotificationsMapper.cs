// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

namespace Neo4j.Driver.TestKitBackend.Messages;

internal interface INotificationsMapper
{
    void Apply(
        string? minimumSeverity,
        string[]? disabledCategories,
        Action disableNotifications,
        Action<Severity?, Category[]?> setNotifications);
}

internal class NotificationsMapper : INotificationsMapper
{
    public void Apply(
        string? minimumSeverity,
        string[]? disabledCategories,
        Action disableNotifications,
        Action<Severity?, Category[]?> setNotifications)
    {
        if (minimumSeverity is null && disabledCategories is null)
        {
            return;
        }

        if (minimumSeverity == "OFF")
        {
            disableNotifications();
            return;
        }

        var severity = minimumSeverity is { } value ? Enum.Parse<Severity>(value, true) : (Severity?)null;
        var categories = disabledCategories?.Select(c => Enum.Parse<Category>(c, true)).ToArray();

        setNotifications(severity, categories);
    }
}
