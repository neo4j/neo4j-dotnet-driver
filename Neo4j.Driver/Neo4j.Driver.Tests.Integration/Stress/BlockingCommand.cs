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

namespace Neo4j.Driver.IntegrationTests.Stress;

public abstract class BlockingCommand : IBlockingCommand
{
    private readonly IDriver _driver;
    private readonly bool _useBookmark;

    protected BlockingCommand(IDriver driver, bool useBookmark)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _useBookmark = useBookmark;
    }

    public abstract void Execute(StressTestContext context);

    protected ISession NewSession(AccessMode mode, StressTestContext context)
    {
        return _driver.Session(
            o =>
                o.WithDefaultAccessMode(mode)
                    .WithBookmarks(_useBookmark ? new[] { context.Bookmarks } : Array.Empty<Bookmarks>()));
    }
}
