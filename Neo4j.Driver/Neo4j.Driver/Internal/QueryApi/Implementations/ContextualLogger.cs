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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

internal class ContextualLogger : ILogger
{
    private readonly ILoggingContext[] _contexts;
    private ILogger _downstream;

    public ContextualLogger(
        IEnumerable<ILoggingContext> contexts,
        ILogger downstream)
    {
        _contexts = contexts.ToArray();
        _downstream = downstream;
        
        var contextNames = string.Join(", ", _contexts.Select(c => $"[{c.Key}:{c.Value}]"));
        _downstream.Trace("Contexts: {contexts}", contextNames);
    }

    private (string, object[]) Contextualise(string messageTemplate, params object[] args)
    {
        var messageSegments = new string[_contexts.Length + 1];
        messageSegments[^1] = messageTemplate;
        
        var allArgs = new object[_contexts.Length + args.Length];
        args.CopyTo(allArgs, _contexts.Length);
        
        for (var index = 0; index < _contexts.Length; index++)
        {
            var context = _contexts[index];
            messageSegments[index + 1] = $"[{context.Key}:{{{context.Key}}}] ";
            
            allArgs[index] = context.Value;
        }
        
        var message = string.Join("", messageSegments);
        return (message, allArgs);
    }

    public void Trace(string messageTemplate, params object[] args)
    {
        (messageTemplate, args) = Contextualise(messageTemplate, args);
        _downstream.Debug(messageTemplate, args);
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
