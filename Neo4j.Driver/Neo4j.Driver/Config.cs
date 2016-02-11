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

using Neo4j.Driver.Internal;

namespace Neo4j.Driver
{
    /// <summary>
    /// Use this class to config the <see cref="Driver"/> in a certain way
    /// </summary>
    public class Config
    {
        /// <summary>
        /// When the <see cref="IdleSessionPoolSize"/> is set to <see cref="InfiniteIdleSessionPoolSize" /> the idle session pool will pool all sessions created by the driver.
        /// </summary>
        public const int InfiniteIdleSessionPoolSize = -1;
        static Config()
        {
            DefaultConfig = new Config
            {
                TlsEnabled = false,
                Logger = new DebugLogger {Level = LogLevel.Info},
                IdleSessionPoolSize = 20
            };
        }

        /// <summary>
        /// Returns the default configuration for the <see cref="Driver"/>
        /// </summary>
        /// <remarks>
        /// The defaults are <br/>
        /// <list type="bullet">
        /// <item><see cref="TlsEnabled"/> : <c>false</c></item>
        /// <item><see cref="Logger"/> : <c>DebugLogger</c> at <c><see cref="LogLevel"/> Info</c> </item>
        /// <item><see cref="IdleSessionPoolSize"/> : <c>20</c> </item>
        /// </list>
        /// </remarks>
        public static Config DefaultConfig { get; }

        /// <summary>
        /// Create an instance of <see cref="IConfigBuilder"/> to build a <see cref="Config"/>
        /// </summary>
        public static IConfigBuilder Builder => new ConfigBuilder(new Config());

        /// <summary>
        /// Gets or sets the use of TLS for all the connections created by the Driver.
        /// </summary>
        public bool TlsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ILogger"/> instance to be used by the <see cref="ISession"/>s.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the idle session pool size.
        /// </summary>
        /// <remarks> The idle session pool size represents the maximum number of sessions buffered for the driver. A buffered <see cref="ISession"/> 
        /// is a session that has already been connected to the database instance and doesn't need to re-initialize.</remarks>
        public int IdleSessionPoolSize { get; set; }

        private class ConfigBuilder : IConfigBuilder
        {
            private readonly Config _config;

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

            public IConfigBuilder WithIdleSessionPoolSize(int size)
            {
                _config.IdleSessionPoolSize = size;
                return this;
            }

            public Config ToConfig()
            {
                return _config;
            }
        }
    }
    /// <summary>
    /// Provides a way to generate a <see cref="Config"/> instance fluently.
    /// </summary>
    public interface IConfigBuilder
    {
        /// <summary>
        /// Builds the <see cref="Config"/> instance based on the previously set values.
        /// </summary>
        /// <remarks>>
        /// If no value was set for a property the defaults specified in <see cref="Config.DefaultConfig"/> will be .
        /// </remarks>
        /// <returns>A <see cref="Config"/> instance.</returns>
        Config ToConfig();
        
        /// <summary>
        /// Sets the <see cref="Config"/> to use TLS if <paramref name="enableTls"/> is <c>true</c>.
        /// </summary>
        /// <param name="enableTls"><c>true</c> enables TLS for the connection, <c>false</c> otherwise.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithTlsEnabled(bool enableTls);

        /// <summary>
        /// Sets the <see cref="Config"/> to use a given <see cref="ILogger"/> instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance to use, if <c>null</c> no logging will occur.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithLogger(ILogger logger);
        
        /// <summary>
        /// Sets the size of the idle session pool.
        /// </summary>
        /// <param name="size">The size of the <see cref="Config.IdleSessionPoolSize"/>, set to <see cref="Config.InfiniteIdleSessionPoolSize"/> to pool all sessions.</param>
        /// <returns>An <see cref="IConfigBuilder"/> instance for further configuration options.</returns>
        /// <remarks>Must call <see cref="ToConfig"/> to generate a <see cref="Config"/> instance.</remarks>
        IConfigBuilder WithIdleSessionPoolSize(int size);
    }

  
}