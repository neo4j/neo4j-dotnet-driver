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

using System.Collections.Generic;
using System.IO;

namespace Neo4j.Driver.IntegrationTests.Internals;

internal static class SettingsHelper
{
    public const string ListenAddr = "dbms.connectors.default_listen_address";
    public const string Ipv6EnabledAddr = "::";

    /// <summary>Updates the settings of the Neo4j server</summary>
    /// <param name="location">Path of the Neo4j server</param>
    /// <param name="keyValuePair">Settings</param>
    public static void UpdateSettings(string location, IDictionary<string, string> keyValuePair)
    {
        var keyValuePairCopy = new Dictionary<string, string>(keyValuePair);

        // rename the old file to a temp file
        var configFileName = Path.Combine(location, "conf/neo4j.conf");
        var tempFileName = Path.Combine(location, "conf/neo4j.conf.tmp");
        File.Move(configFileName, tempFileName);

        using var reader = new StreamReader(new FileStream(tempFileName, FileMode.Open, FileAccess.Read));
        using var writer =
            new StreamWriter(new FileStream(configFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite));

        while (reader.ReadLine() is { } line)
        {
            if (line.Trim() == string.Empty || line.Trim().StartsWith("#"))
            {
                // empty or comments, print as original
                writer.WriteLine(line);
            }
            else
            {
                var tokens = line.Split('=');
                if (tokens.Length == 2 && keyValuePairCopy.ContainsKey(tokens[0].Trim()))
                {
                    var key = tokens[0].Trim();
                    // found property and update it to the new value
                    writer.WriteLine($"{key}={keyValuePairCopy[key]}");
                    keyValuePairCopy.Remove(key);
                }
                else
                {
                    // not the property that we are looking for, print it as original
                    writer.WriteLine(line);
                }
            }
        }

        // write the extra properties at the end of the file
        foreach (var pair in keyValuePairCopy)
        {
            writer.WriteLine($"{pair.Key}={pair.Value}");
        }

        // delete the temp file
        File.Delete(tempFileName);
    }
}
