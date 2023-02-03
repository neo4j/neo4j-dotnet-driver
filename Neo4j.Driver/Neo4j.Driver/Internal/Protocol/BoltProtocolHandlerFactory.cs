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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.V3;
using Neo4j.Driver.Internal.MessageHandling.V4;
using Neo4j.Driver.Internal.Result;

namespace Neo4j.Driver.Internal;

internal interface IBoltProtocolHandlerFactory
{
    IResultCursorBuilder NewResultCursorBuilder(
        SummaryBuilder summaryBuilder,
        IConnection connection,
        Func<IConnection, SummaryBuilder, IBookmarksTracker, Func<IResultStreamBuilder, long, long, Task>> requestMore,
        Func<IConnection, SummaryBuilder, IBookmarksTracker, Func<IResultStreamBuilder, long, Task>> cancelRequest,
        IBookmarksTracker bookmarksTracker,
        IResultResourceHandler resultResourceHandler,
        long fetchSize,
        bool reactive);

    RunResponseHandler NewRunResponseHandler(IResultCursorBuilder streamBuilder, SummaryBuilder summaryBuilder);

    PullResponseHandler NewPullResponseHandler(
        IBookmarksTracker bookmarksTracker,
        IResultStreamBuilder cursorBuilder,
        SummaryBuilder summaryBuilder);

    RouteResponseHandler NewRouteResponseHandler();
    HelloResponseHandler NewHelloResponseHandler(IConnection connection);
    HelloResponseHandlerV51 NewHelloResponseHandlerV51(IConnection connection);

    LogonResponseHandler NewLogonResponseHandler(IConnection connection);

    CommitResponseHandler NewCommitResponseHandler(IBookmarksTracker bookmarksTracker);

    //Bolt V3
    RunResponseHandlerV3 NewRunResponseHandlerV3(IResultCursorBuilder streamBuilder, SummaryBuilder summaryBuilder);

    PullAllResponseHandler NewPullAllResponseHandler(
        IResultCursorBuilder streamBuilder,
        SummaryBuilder summaryBuilder,
        IBookmarksTracker bookmarksTracker);
}

internal class BoltProtocolHandlerFactory : IBoltProtocolHandlerFactory
{
    internal static readonly BoltProtocolHandlerFactory Instance = new();

    public IResultCursorBuilder NewResultCursorBuilder(
        SummaryBuilder summaryBuilder,
        IConnection connection,
        Func<IConnection, SummaryBuilder, IBookmarksTracker, Func<IResultStreamBuilder, long, long, Task>> requestMore,
        Func<IConnection, SummaryBuilder, IBookmarksTracker, Func<IResultStreamBuilder, long, Task>> cancelRequest,
        IBookmarksTracker bookmarksTracker,
        IResultResourceHandler resultResourceHandler,
        long fetchSize,
        bool reactive)
    {
        return new ResultCursorBuilder(
            summaryBuilder,
            connection.ReceiveOneAsync,
            requestMore?.Invoke(connection, summaryBuilder, bookmarksTracker),
            cancelRequest?.Invoke(connection, summaryBuilder, bookmarksTracker),
            resultResourceHandler,
            fetchSize,
            reactive);
    }

    public RunResponseHandler NewRunResponseHandler(IResultCursorBuilder streamBuilder, SummaryBuilder summaryBuilder)
    {
        return new RunResponseHandler(streamBuilder, summaryBuilder);
    }

    public PullResponseHandler NewPullResponseHandler(
        IBookmarksTracker bookmarksTracker,
        IResultStreamBuilder cursorBuilder,
        SummaryBuilder summaryBuilder)
    {
        return new PullResponseHandler(cursorBuilder, summaryBuilder, bookmarksTracker);
    }

    public RouteResponseHandler NewRouteResponseHandler()
    {
        return new RouteResponseHandler();
    }

    public HelloResponseHandler NewHelloResponseHandler(IConnection connection)
    {
        return new HelloResponseHandler(connection);
    }

    public HelloResponseHandlerV51 NewHelloResponseHandlerV51(IConnection connection)
    {
        return new HelloResponseHandlerV51(connection);
    }

    public LogonResponseHandler NewLogonResponseHandler(IConnection connection)
    {
        return new LogonResponseHandler(connection);
    }

    public CommitResponseHandler NewCommitResponseHandler(IBookmarksTracker bookmarksTracker)
    {
        return new CommitResponseHandler(bookmarksTracker);
    }

    public RunResponseHandlerV3 NewRunResponseHandlerV3(
        IResultCursorBuilder streamBuilder,
        SummaryBuilder summaryBuilder)
    {
        return new RunResponseHandlerV3(streamBuilder, summaryBuilder);
    }

    public PullAllResponseHandler NewPullAllResponseHandler(
        IResultCursorBuilder streamBuilder,
        SummaryBuilder summaryBuilder,
        IBookmarksTracker bookmarksTracker)
    {
        return new PullAllResponseHandler(streamBuilder, summaryBuilder, bookmarksTracker);
    }
}
