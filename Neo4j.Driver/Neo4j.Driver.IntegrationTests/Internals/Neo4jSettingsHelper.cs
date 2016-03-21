using System.Collections.Generic;
using System.IO;

namespace Neo4j.Driver.IntegrationTests.Internals
{
  internal static class Neo4jSettingsHelper
  {
    /// <summary>
    /// Updates the settings of the Neo4j server
    /// </summary>
    /// <param name="location">Path of the Neo4j server</param>
    /// <param name="keyValuePair">Settings</param>
    public static void UpdateSettings(string location, IDictionary<string, string> keyValuePair)
    {
      var keyValuePairCopy = new Dictionary<string, string>(keyValuePair);

      // rename the old file to a temp file
      var configFileName = Path.Combine(location, "conf/neo4j.conf");
      var tempFileName = Path.Combine(location, "conf/neo4j.conf.tmp");
      File.Move(configFileName, tempFileName);

      using (var reader = new StreamReader(tempFileName))
      using (var writer = new StreamWriter(configFileName))
      {
        string line;
        while ((line = reader.ReadLine()) != null)
        {
          if (line.Trim() == string.Empty || line.Trim().StartsWith("#"))
          {
            // empty or comments, print as original
            writer.WriteLine(line);
          }
          else
          {
            string[] tokens = line.Split('=');
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

        // write the extral propertes at the end of the file
        foreach (var pair in keyValuePairCopy)
        {
          writer.WriteLine($"{pair.Key}={pair.Value}");
        }
      }
      // delete the temp file
      File.Delete(tempFileName);
    }
  }
}
