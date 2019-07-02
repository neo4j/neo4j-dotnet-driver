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

using System.Collections.Generic;
using Neo4j.Driver.Internal;

namespace Neo4j.Driver
{
    /// <summary>
    /// An executable statement, i.e. the statements' text and its parameters.
    /// </summary>
    public class Statement
    {
        private static IDictionary<string, object> NoParameter { get; }

        static Statement()
        {
            NoParameter = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the statement's text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the statement's parameters.
        /// </summary>
        public IDictionary<string, object> Parameters { get; }

        /// <summary>
        /// Create a statement with no statement parameters.
        /// </summary>
        /// <param name="text">The statement's text</param>
        public Statement(string text) : this(text, (object) null)
        {
        }
        /// <summary>
        /// Create a statement with parameters specified as anonymous objects
        /// </summary>
        /// <param name="text">The statement's text</param>
        public Statement(string text, object parameters)
            : this(text, parameters.ToDictionary())
        {
        }

        /// <summary>
        /// Create a statement
        /// </summary>
        /// <param name="text">The statement's text</param>
        /// <param name="parameters">The statement's parameters, whose values should not be changed while the statement is used in a session/transaction.</param>
        public Statement(string text, IDictionary<string, object> parameters)
        {
            Text = text;
            Parameters = parameters ?? NoParameter;
        }

        /// <summary>
        /// Print the statement.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"`{Text}`, {Parameters.ToContentString()}";
        }
    }
}