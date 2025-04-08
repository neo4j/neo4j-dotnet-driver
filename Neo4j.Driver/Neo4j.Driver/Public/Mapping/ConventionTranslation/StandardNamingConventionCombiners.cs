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

using System.Collections.Generic;
using System.Linq;

namespace Neo4j.Driver.Mapping.ConventionTranslation;

public class SnakeCaseCombiner : ITokenCombiner
{
    public string CombineTokens(IEnumerable<string> tokens)
    {
        return string.Join("_", tokens.Select(t => t.ToLower()));
    }
}

public class CamelCaseCombiner : ITokenCombiner
{
    public string CombineTokens(IEnumerable<string> tokens)
    {
        var tokenList = tokens.ToList();
        if (tokenList.Count == 0)
        {
            return string.Empty;
        }

        var firstToken = tokenList[0].ToLower();
        var restTokens = tokenList.Skip(1).Select(t => char.ToUpper(t[0]) + t.Substring(1).ToLower());

        return firstToken + string.Concat(restTokens);
    }
}

public class PascalCaseCombiner : ITokenCombiner
{
    public string CombineTokens(IEnumerable<string> tokens)
    {
        return string.Concat(tokens.Select(t => char.ToUpper(t[0]) + t.Substring(1).ToLower()));
    }
}

public class ScreamingSnakeCaseCombiner : ITokenCombiner
{
    public string CombineTokens(IEnumerable<string> tokens)
    {
        return string.Join("_", tokens.Select(t => t.ToUpper()));
    }
}

public class KebabCaseCombiner : ITokenCombiner
{
    public string CombineTokens(IEnumerable<string> tokens)
    {
        return string.Join("-", tokens.Select(t => t.ToLower()));
    }
}
