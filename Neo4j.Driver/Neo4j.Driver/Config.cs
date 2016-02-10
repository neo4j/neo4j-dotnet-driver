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
namespace Neo4j.Driver
{
    /// <summary>
    /// Use this class to config the <see cref="Driver"/> in a certain way
    /// </summary>
    public class Config
    {
        public const int InfiniteSessionPoolSize = 0;
        static Config()
        {
            DefaultConfig = new Config
            {
                TlsEnabled = false,
                Logger = new DebugLogger {Level = LogLevel.Info},
                MaxSessionPoolSize = 20
            };
        }

        /// <summary>
        /// Returns the default configuration for the <see cref="Driver"/>
        /// </summary>
        /// <value>The default configuration for the
        ///   <see cref="Driver"/></value>
        public static Config DefaultConfig { get; }

        public static IConfigBuilder Builder => new ConfigBuilder(new Config());

        public bool TlsEnabled { get; set; }

        public ILogger Logger { get; set; }

        public int MaxSessionPoolSize { get; set; }

        private class ConfigBuilder : IConfigBuilder
        {
            private Config _config;

            internal ConfigBuilder(Config config)
            {
                _config = config;
            }

            public IConfigBuilder WithTlsEnabled(bool enableTls)
            {
                _config.TlsEnabled = enableTls;
                return this;
            }

            public IConfigBuilder WithLogger(ILogger logger)
            {
                _config.Logger = logger;
                return this;
            }

            public IConfigBuilder WithMaxSessionPoolSize(int size)
            {
                _config.MaxSessionPoolSize = size;
                return this;
            }

            public Config ToConfig()
            {
                return _config;
            }
        }
    }
    public interface IConfigBuilder
    {
        Config ToConfig();
        IConfigBuilder WithTlsEnabled(bool enableTls);
        IConfigBuilder WithLogger(ILogger logger);
        IConfigBuilder WithMaxSessionPoolSize(int size);
    }

  
}