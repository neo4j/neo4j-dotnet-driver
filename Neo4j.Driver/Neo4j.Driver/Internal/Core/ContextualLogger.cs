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

using System;

namespace Neo4j.Driver.Internal;

internal class ContextualLogger : ILogger
{
    private readonly ILoggingContextTracker _tracker;
    private ILogger _downstream;

    public ContextualLogger(
        ILoggingContextTracker tracker,
        ILogger downstream)
    {
        _tracker = tracker;
        _downstream = downstream;
    }

    private (string, object[]) Contextualise(string messageTemplate, params object[] args)
    {
        var contexts = _tracker.Contexts;
        var messageSegments = new string[contexts.Count + 1];
        messageSegments[^1] = messageTemplate;

        var allArgs = new object[contexts.Count + args.Length];
        args.CopyTo(allArgs, contexts.Count);

        for (var index = 0; index < contexts.Count; index++)
        {
            var context = contexts[index];
            messageSegments[index] = $"[{context.Key}:{{{context.Key}}}] ";
            allArgs[index] = context.Value;
        }

        var message = string.Join("", messageSegments);
        return (message, allArgs);
    }

    public void Trace(string messageTemplate, params object[] args)
    {
        (messageTemplate, args) = Contextualise(messageTemplate, args);
        _downstream.Trace(messageTemplate, args);
    }

    public void Debug(string messageTemplate, params object[] args)
    {
        (messageTemplate, args) = Contextualise(messageTemplate, args);
        _downstream.Debug(messageTemplate, args);
    }

    public void Info(string messageTemplate, params object[] args)
    {
        (messageTemplate, args) = Contextualise(messageTemplate, args);
        _downstream.Info(messageTemplate, args);
    }

    public void Warn(string messageTemplate, params object[] args)
    {
        (messageTemplate, args) = Contextualise(messageTemplate, args);
        _downstream.Warn(messageTemplate, args);
    }

    public void Warn(Exception exception, string messageTemplate, params object[] args)
    {
        (messageTemplate, args) = Contextualise(messageTemplate, args);
        _downstream.Warn(exception, messageTemplate, args);
    }

    public void Error(string messageTemplate, params object[] args)
    {
        (messageTemplate, args) = Contextualise(messageTemplate, args);
        _downstream.Error(messageTemplate, args);
    }

    public void Error(Exception exception, string messageTemplate, params object[] args)
    {
        (messageTemplate, args) = Contextualise(messageTemplate, args);
        _downstream.Error(exception, messageTemplate, args);
    }
}
