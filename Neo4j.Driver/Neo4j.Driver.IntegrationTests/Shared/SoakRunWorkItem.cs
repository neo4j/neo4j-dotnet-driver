// Copyright (c) 2002-2018 "Neo Technology,"
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    internal class SoakRunWorkItem
    {
        private static readonly string[] queries = new[]
        {
            "RETURN 1295 + 42",
            "UNWIND range(1,10000) AS x CREATE (n {prop:x}) DELETE n RETURN sum(x)",
            @"UNWIND RANGE(1,500) AS N RETURN N, null, true, false, -2147483650, -32770, -130, -18, 126, 32765, 2147483645, 2147483650, 1.1, -1.1, """", ""1"", ""1234567890"", 
                ""ppkXUYYqy9ENl9caIvQyRsO19jXy4LhZHHVZMyUBQjbHJXlkv2vvUfeowOOujxTxJxXufzzGLFjn35w4zFpR4pRnlM3a1AkwV0L6jCj0pWic7bPBwiiasjJTieiynvE9CIHBwdyfGAUW4YMrGR5mBZwkMWIZlDQ9
                    hmLD3ohSlzoYp8ilj4pDreho83L5eSobYOGOKMn3ytLIKawzjO21eCIVAAXT5DmESMfoTixDLQbi9HGrNEzm56NLQjeTEsKxEKPHFDkHUWBscBIJyJsdYbObe1DgxOhWoxYz3i4cft5iL3IpsaELdo5ES3LVJP
                    0kKh0FyFI2HTqRjNPWBWgQZyKX2uh0zoOhVApNdhQBl3mXZmaSuETsMzwdvewHOLOKp1tB1gCgvRXNf4OQ4XLOnSk6ksYW7Pd0NXEwhmFpgXTtQuJdDYXUwQD73oqq56ug6yX44MuscfdtDwJvuioBgkeItWcR
                    FygHmLkHFNeIloqMU0nxTeUoaoA4yzyPybiQTjSU0e9CgCxkODc1ynQGIFCzo6XUws5mz31LIDmGssFT4tqxgeDH9aCjHnWTcIMB46i2JEOonXfqOsCKrvvoJBlslQvilNPitcwOIgbmRUv0iOfWXb7EOmutFA
                    ZSs3Oeo5DGB1lkxRdZgidkV5fyfabYhR1Rd7jZsK7Sr4nzSSpnwuHjGRzqNd4Nwvi2Y94q6OuA17PVtwvordaJraTbCtOdkbCZPU7UsJfwlSOCCJM5XFmvIvquDTlyeNcaFypnADkEKcspqIqfxmECH9uH6H9I
                    CfQ5xeOzf1Cl1TqsDnWWQWmyrerFAu5IAaCmILWUaZwQTMHkI2vteeh9eigYjWEak23vD9DkGCB8KBfzVzHsIN0fidBEsGqE7wXg1CZWfLhRqu50WD676dKP61mVBeeyHRPyhtuZ7KLnETp0B9tYztzCe65pc9
                    lXyy1jjLGdMIrJfKPiSLC4q9C2O5qyxJMscO8D2ZgEpZve144x"", RANGE(0,100), {{KEY_1: 1, KEY_2: 2, KEY_3: 3, KEY_4: 4, KEY_5: 5, KEY_6: 6, KEY_7: 7, KEY_8: 8, KEY_9: 9, 
                    KEY_10: 10, KEY_11: 11, KEY_12: 12, KEY_13: 13, KEY_14: 14, KEY_15: 15, KEY_16: 16, KEY_17: 17, KEY_18: 18, KEY_19: 19, KEY_20: 20, KEY_21: 21, KEY_22: 22, 
                    KEY_23: 23, KEY_24: 24, KEY_25: 25, KEY_26: 26, KEY_27: 27, KEY_28: 28, KEY_29: 29, KEY_30: 30, KEY_31: 31, KEY_32: 32, KEY_33: 33, KEY_34: 34, KEY_35: 35, 
                    KEY_36: 36, KEY_37: 37, KEY_38: 38, KEY_39: 39, KEY_40: 40, KEY_41: 41, KEY_42: 42, KEY_43: 43, KEY_44: 44, KEY_45: 45, KEY_46: 46, KEY_47: 47, KEY_48: 48, 
                    KEY_49: 49, KEY_50: 50, KEY_51: 51, KEY_52: 52, KEY_53: 53, KEY_54: 54, KEY_55: 55, KEY_56: 56, KEY_57: 57, KEY_58: 58, KEY_59: 59, KEY_60: 60, KEY_61: 61, 
                    KEY_62: 62, KEY_63: 63, KEY_64: 64, KEY_65: 65, KEY_66: 66, KEY_67: 67, KEY_68: 68, KEY_69: 69, KEY_70: 70, KEY_71: 71, KEY_72: 72, KEY_73: 73, KEY_74: 74, 
                    KEY_75: 75, KEY_76: 76, KEY_77: 77, KEY_78: 78, KEY_79: 79, KEY_80: 80, KEY_81: 81, KEY_82: 82, KEY_83: 83, KEY_84: 84, KEY_85: 85, KEY_86: 86, KEY_87: 87, 
                    KEY_88: 88, KEY_89: 89, KEY_90: 90, KEY_91: 91, KEY_92: 92, KEY_93: 93, KEY_94: 94, KEY_95: 95, KEY_96: 96, KEY_97: 97, KEY_98: 98, KEY_99: 99, KEY_100: 100}}",
            @"UNWIND RANGE(1,5000) AS N RETURN N, null, true, false, -2147483650, -32770, -130, -18, 126, 32765, 2147483645, 2147483650, 1.1, -1.1, """", ""1"", ""1234567890"", 
                ""ppkXUYYqy9ENl9caIvQyRsO19jXy4LhZHHVZMyUBQjbHJXlkv2vvUfeowOOujxTxJxXufzzGLFjn35w4zFpR4pRnlM3a1AkwV0L6jCj0pWic7bPBwiiasjJTieiynvE9CIHBwdyfGAUW4YMrGR5mBZwkMWIZlDQ9
                    hmLD3ohSlzoYp8ilj4pDreho83L5eSobYOGOKMn3ytLIKawzjO21eCIVAAXT5DmESMfoTixDLQbi9HGrNEzm56NLQjeTEsKxEKPHFDkHUWBscBIJyJsdYbObe1DgxOhWoxYz3i4cft5iL3IpsaELdo5ES3LVJP
                    0kKh0FyFI2HTqRjNPWBWgQZyKX2uh0zoOhVApNdhQBl3mXZmaSuETsMzwdvewHOLOKp1tB1gCgvRXNf4OQ4XLOnSk6ksYW7Pd0NXEwhmFpgXTtQuJdDYXUwQD73oqq56ug6yX44MuscfdtDwJvuioBgkeItWcR
                    FygHmLkHFNeIloqMU0nxTeUoaoA4yzyPybiQTjSU0e9CgCxkODc1ynQGIFCzo6XUws5mz31LIDmGssFT4tqxgeDH9aCjHnWTcIMB46i2JEOonXfqOsCKrvvoJBlslQvilNPitcwOIgbmRUv0iOfWXb7EOmutFA
                    ZSs3Oeo5DGB1lkxRdZgidkV5fyfabYhR1Rd7jZsK7Sr4nzSSpnwuHjGRzqNd4Nwvi2Y94q6OuA17PVtwvordaJraTbCtOdkbCZPU7UsJfwlSOCCJM5XFmvIvquDTlyeNcaFypnADkEKcspqIqfxmECH9uH6H9I
                    CfQ5xeOzf1Cl1TqsDnWWQWmyrerFAu5IAaCmILWUaZwQTMHkI2vteeh9eigYjWEak23vD9DkGCB8KBfzVzHsIN0fidBEsGqE7wXg1CZWfLhRqu50WD676dKP61mVBeeyHRPyhtuZ7KLnETp0B9tYztzCe65pc9
                    lXyy1jjLGdMIrJfKPiSLC4q9C2O5qyxJMscO8D2ZgEpZve144x"", RANGE(0,100), {{KEY_1: 1, KEY_2: 2, KEY_3: 3, KEY_4: 4, KEY_5: 5, KEY_6: 6, KEY_7: 7, KEY_8: 8, KEY_9: 9, 
                    KEY_10: 10, KEY_11: 11, KEY_12: 12, KEY_13: 13, KEY_14: 14, KEY_15: 15, KEY_16: 16, KEY_17: 17, KEY_18: 18, KEY_19: 19, KEY_20: 20, KEY_21: 21, KEY_22: 22, 
                    KEY_23: 23, KEY_24: 24, KEY_25: 25, KEY_26: 26, KEY_27: 27, KEY_28: 28, KEY_29: 29, KEY_30: 30, KEY_31: 31, KEY_32: 32, KEY_33: 33, KEY_34: 34, KEY_35: 35, 
                    KEY_36: 36, KEY_37: 37, KEY_38: 38, KEY_39: 39, KEY_40: 40, KEY_41: 41, KEY_42: 42, KEY_43: 43, KEY_44: 44, KEY_45: 45, KEY_46: 46, KEY_47: 47, KEY_48: 48, 
                    KEY_49: 49, KEY_50: 50, KEY_51: 51, KEY_52: 52, KEY_53: 53, KEY_54: 54, KEY_55: 55, KEY_56: 56, KEY_57: 57, KEY_58: 58, KEY_59: 59, KEY_60: 60, KEY_61: 61, 
                    KEY_62: 62, KEY_63: 63, KEY_64: 64, KEY_65: 65, KEY_66: 66, KEY_67: 67, KEY_68: 68, KEY_69: 69, KEY_70: 70, KEY_71: 71, KEY_72: 72, KEY_73: 73, KEY_74: 74, 
                    KEY_75: 75, KEY_76: 76, KEY_77: 77, KEY_78: 78, KEY_79: 79, KEY_80: 80, KEY_81: 81, KEY_82: 82, KEY_83: 83, KEY_84: 84, KEY_85: 85, KEY_86: 86, KEY_87: 87, 
                    KEY_88: 88, KEY_89: 89, KEY_90: 90, KEY_91: 91, KEY_92: 92, KEY_93: 93, KEY_94: 94, KEY_95: 95, KEY_96: 96, KEY_97: 97, KEY_98: 98, KEY_99: 99, KEY_100: 100}}",
        };

        private static readonly AccessMode[] accessModes = new[]
        {
            AccessMode.Read,
            AccessMode.Write
        };

        private readonly ITestOutputHelper _output;
        private readonly IDriver _driver;
        private readonly IStatisticsCollector _collector;
        private int _counter;

        public SoakRunWorkItem(IDriver driver, IStatisticsCollector collector, ITestOutputHelper output)
        {
            this._driver = driver;
            this._collector = collector;
            this._output = output;
        }

        public Task Run()
        {
            return Task.Run(() =>
            {
                var currentIteration = Interlocked.Increment(ref _counter);
                var query = queries[currentIteration % queries.Length];
                var accessMode = accessModes[currentIteration % accessModes.Length];

                using (var session = _driver.Session(accessMode))
                {
                    try
                    {
                        var result = session.Run(query);
                        if (currentIteration % 1000 == 0)
                        {
                            _output.WriteLine(_collector.CollectStatistics().ToContentString());
                        }

                        result.Consume();
                    }
                    catch (Exception e)
                    {
                        _output.WriteLine(
                            $"[{DateTime.Now:HH:mm:ss.ffffff}] Iteration {currentIteration} failed to run query {query} due to {e.Message}");
                    }
                }
            });
        }

        public async Task RunAsync()
        {
            var currentIteration = Interlocked.Increment(ref _counter);
            var query = queries[currentIteration % queries.Length];
            var accessMode = accessModes[currentIteration % accessModes.Length];

            var session = _driver.Session(accessMode);
            try
            {

                var result = await session.RunAsync(query);
                if (currentIteration % 1000 == 0)
                {
                    _output.WriteLine(_collector.CollectStatistics().ToContentString());
                }
                await result.SummaryAsync();
            }
            catch (Exception e)
            {
                _output.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss.ffffff}] Iteration {currentIteration} failed to run query {query} due to {e.Message}");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

    }

}
