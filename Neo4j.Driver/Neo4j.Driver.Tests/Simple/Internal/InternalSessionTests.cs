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

using Moq;
using Neo4j.Driver.Internal;
using Xunit;

namespace Neo4j.Driver.Tests.Simple.Internal;

public static class InternalSessionTests
{
    public class LastBookmarks
    {
        [Fact]
        public void ShouldDelegateToAsyncSession()
        {
            var asyncSession = new Mock<IInternalAsyncSession>();
            var session = new InternalSession(
                asyncSession.Object,
                Mock.Of<IRetryLogic>(),
                Mock.Of<BlockingExecutor>());

            var bookmark = session.LastBookmarks;

            asyncSession.Verify(x => x.LastBookmarks, Times.Once);
        }
    }

    public class LastBookmark
    {
        [Fact]
        public void ShouldDelegateToAsyncSession()
        {
            var asyncSession = new Mock<IInternalAsyncSession>();
            var session = new InternalSession(
                asyncSession.Object,
                Mock.Of<IRetryLogic>(),
                Mock.Of<BlockingExecutor>());

            var bookmark = session.LastBookmark;

#pragma warning disable CS0618
            asyncSession.Verify(x => x.LastBookmark, Times.Once);
#pragma warning restore CS0618
        }
    }

    public class SessionConfig
    {
        [Fact]
        public void ShouldDelegateToAsyncSession()
        {
            var asyncSession = new Mock<IInternalAsyncSession>();
            var session = new InternalSession(
                asyncSession.Object,
                Mock.Of<IRetryLogic>(),
                Mock.Of<BlockingExecutor>());

            var config = session.SessionConfig;

            asyncSession.Verify(x => x.SessionConfig, Times.Once);
        }
    }
}
