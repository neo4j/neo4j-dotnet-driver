﻿// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using Neo4j.Driver.Internal.Connector;

namespace Neo4j.Driver.Internal
{
    internal class SessionPool : LoggerBase
    {
        private readonly IAuthToken _authToken;
        private readonly Queue<IPooledSession> _availableSessions = new Queue<IPooledSession>();
        private readonly Config _config;
        private readonly IConnection _connection;
        private readonly int _idleSessionPoolSize;
        private readonly Dictionary<Guid, IPooledSession> _inUseSessions = new Dictionary<Guid, IPooledSession>();
        private readonly Uri _uri;

        public SessionPool(Uri uri, IAuthToken authToken, ILogger logger, Config config, IConnection connection = null)
            : base(logger)
        {
            _uri = uri;
            _authToken = authToken;
            _config = config;
            _connection = connection;
            _idleSessionPoolSize = config.MaxIdleSessionPoolSize;
        }

        internal SessionPool(
            Queue<IPooledSession> availableSessions,
            Dictionary<Guid, IPooledSession> inUseDictionary,
            IConnection connection = null,
            ILogger logger = null)
            : this(null, AuthTokens.None, logger, Config.DefaultConfig, connection)
        {
            _availableSessions = availableSessions ?? new Queue<IPooledSession>();
            _inUseSessions = inUseDictionary ?? new Dictionary<Guid, IPooledSession>();
        }

        internal int NumberOfInUseSessions => _inUseSessions.Count;
        internal int NumberOfAvailableSessions => _availableSessions.Count;

        public ISession GetSession()
        {
            return TryExecute(() =>
            {
                IPooledSession session = null;
                lock (_availableSessions)
                {
                    if (_availableSessions.Count != 0)
                        session = _availableSessions.Dequeue();
                }

                if (session == null)
                {
                    session = new Session(_uri, _authToken, _config, _connection, Release);
                    lock (_inUseSessions)
                    {
                        _inUseSessions.Add(session.Id, session);
                    }
                    return session;
                }

                if (!session.IsHealthy)
                {
                    session.Close();
                    return GetSession();
                }

                session.Reset();
                lock (_inUseSessions)
                {
                    _inUseSessions.Add(session.Id, session);
                }
                return session;
            });
        }

        public void Release(Guid sessionId)
        {
            TryExecute(() =>
            {
                IPooledSession session;
                lock (_inUseSessions)
                {
                    if (!_inUseSessions.ContainsKey(sessionId))
                    {
                        return;
                    }

                    session = _inUseSessions[sessionId];
                    _inUseSessions.Remove(sessionId);
                }

                if (session.IsHealthy)
                {
                    lock (_availableSessions)
                    {
                        if (_availableSessions.Count < _idleSessionPoolSize ||
                            _idleSessionPoolSize == Config.InfiniteMaxIdleSessionPoolSize)
                        {
                            _availableSessions.Enqueue(session);
                        }
                    }
                }
                else
                {
                    //release resources by session
                    session.Close();
                }
            });
        }

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
            {
                return;
            }

            TryExecute(() =>
            {
                lock (_inUseSessions)
                {
                    var sessions = new List<IPooledSession>(_inUseSessions.Values);
                    _inUseSessions.Clear();
                    foreach (var inUseSession in sessions)
                    {
                        Logger?.Info($"Disposing In Use Session {inUseSession.Id}");
                        inUseSession.Close();
                    }
                }
                lock (_availableSessions)
                {
                    while (_availableSessions.Count > 0)
                    {
                        var session = _availableSessions.Dequeue();
                        Logger?.Info($"Disposing Available Session {session.Id}");
                        session.Close();
                    }
                }
            });
            base.Dispose(true);
        }
    }


    internal interface IPooledSession : ISession
    {
        Guid Id { get; }
        bool IsHealthy { get; }
        void Reset();
        void Close();
    }
}