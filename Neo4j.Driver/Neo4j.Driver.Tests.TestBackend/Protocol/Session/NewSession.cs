// Copyright (c) 2002-2022 "Neo4j,"
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal class NewSession : ProtocolObject
{
    public enum SessionState
    {
        RetryAbleNothing,
        RetryAblePositive,
        RetryAbleNegative
    }

    [JsonIgnore] public SessionState RetryState { get; private set; } = SessionState.RetryAbleNothing;
    [JsonIgnore] public string RetryableErrorId { get; private set; }

    public NewSessionType data { get; set; } = new();
    [JsonIgnore] public IAsyncSession Session { get; set; }

    [JsonIgnore]
    public AccessMode GetAccessMode
    {
        get
        {
            if (data.accessMode == "r")
                return AccessMode.Read;
            return AccessMode.Write;
        }
    }

    [JsonIgnore] public List<string> SessionTransactions { get; } = new();

    private void SessionConfig(SessionConfigBuilder configBuilder)
    {
        if (!string.IsNullOrEmpty(data.database)) configBuilder.WithDatabase(data.database);
        if (!string.IsNullOrEmpty(data.accessMode)) configBuilder.WithDefaultAccessMode(GetAccessMode);
        if (data.bookmarks.Count > 0) configBuilder.WithBookmarks(Bookmarks.From(data.bookmarks.ToArray()));
        if (data.fetchSize.HasValue)
            configBuilder.WithFetchSize(data.fetchSize.Value);
        if (!string.IsNullOrEmpty(data.impersonatedUser)) configBuilder.WithImpersonatedUser(data.impersonatedUser);
    }

    public override async Task ProcessAsync()
    {
        var driver = ((NewDriver) ObjManager.GetObject(data.driverId)).Driver;

        Session = driver.AsyncSession(SessionConfig);

        await Task.CompletedTask;
    }

    public override string Respond()
    {
        return new ProtocolResponse("Session", UniqueId).Encode();
    }

    public void SetupRetryAbleState(SessionState state, string retryableErrorId = "")
    {
        RetryState = state;
        RetryableErrorId = retryableErrorId;
    }

    public class NewSessionType
    {
        public string driverId { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string accessMode { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public List<string> bookmarks { get; set; } = new();

        [JsonProperty(Required = Required.AllowNull)]
        public string database { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public long? fetchSize { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string impersonatedUser { get; set; }
    }
}