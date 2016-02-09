//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using System.Collections.Concurrent;

namespace Neo4j.Driver.Internal
{
    internal class SessionPool : LoggerBase
    {
        private readonly ConcurrentQueue<IPooledSession> _availableSessions = new ConcurrentQueue<IPooledSession>();
        private readonly ConcurrentDictionary<Guid, IPooledSession> _inUseSessions = new ConcurrentDictionary<Guid, IPooledSession>();
        private readonly Uri _uri;
        private readonly Config _config;
        private readonly IConnection _connection;

        internal int NumberOfInUseSessions => _inUseSessions.Count;
        internal int NumberOfAvailableSessions => _availableSessions.Count;

        public SessionPool(ILogger logger, Uri uri, Config config, IConnection connection = null) : base(logger)
        {
            _uri = uri;
            _config = config;
            _connection = connection;
        }

        internal SessionPool(ConcurrentQueue<IPooledSession> availableSessions, Uri uri = null, IConnection connection = null) 
            : this(null, uri, null, connection)
        {
            _availableSessions = availableSessions;
        }
        internal SessionPool(ConcurrentDictionary<Guid, IPooledSession> inUseDictionary) : this(null, null, null, null)
        {
            _inUseSessions = inUseDictionary;
        }

        public ISession GetSession()
        {
            IPooledSession session;
            if (!_availableSessions.TryDequeue(out session) )
            {
                var newSession = new Session(_uri, _config, _connection, Release);
                return _inUseSessions.GetOrAdd(newSession.Id, newSession);
            }
            
            if (!session.IsHealthy())
            {
                session.Close();
                return GetSession();
            }

            session.Reset();
            return _inUseSessions.GetOrAdd(session.Id, session);
        }

        public void Release(Guid sessionId)
        {
            IPooledSession session;
            if (!_inUseSessions.TryRemove(sessionId, out session))
            {
                return;
            }
            if (session.IsHealthy())
            {
                _availableSessions.Enqueue(session);
            }
            else
            {
                //release resources by session
                session.Close();
            }
        }
    }

    public interface IPooledSession : ISession
    {
        Guid Id { get; }
        bool IsHealthy();
        void Reset();
        void Close();
    }
}
