// Copyright (c) 2002-2019 "Neo4j,"
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

using System;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace Neo4j.Driver.Internal.Logging
{
    internal static class DriverLoggerUtil
    {
        public static void TryExecute(IDriverLogger logger, Action action, string message = null)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                logger?.Error(ex, message);
                throw;
            }
        }

        public static T TryExecute<T>(IDriverLogger logger, Func<T> func, string message = null)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                logger?.Error(ex, message);
                throw;
            }
        }

        public static async Task TryExecuteAsync(IDriverLogger logger, Func<Task> func, string message = null)
        {
            try
            {
                await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.Error(ex, message);
                throw;
            }
        }

        public static async Task<T> TryExecuteAsync<T>(IDriverLogger logger, Func<Task<T>> func, string message = null)
        {
            try
            {
                return await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.Error(ex, message);
                throw;
            }
        }
    }
}