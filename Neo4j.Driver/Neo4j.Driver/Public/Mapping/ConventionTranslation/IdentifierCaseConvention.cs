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

namespace Neo4j.Driver.Mapping.ConventionTranslation;

/// <summary>
/// Represents the various identifier naming conventions supported.
/// </summary>
public enum IdentifierCaseConvention
{
    /// <summary>
    /// Represents the C# identifier naming convention, e.g. exampleFieldName or ExampleFieldName.
    /// </summary>
    CSharpIdentifier,

    /// <summary>
    /// Represents the camel case convention, e.g. exampleFieldName.
    /// </summary>
    CamelCase,

    /// <summary>
    /// Represents the pascal case convention, e.g. ExampleFieldName.
    /// </summary>
    PascalCase,

    /// <summary>
    /// Represents the snake case convention, e.g. example_field_name.
    /// </summary>
    SnakeCase,

    /// <summary>
    /// Represents the screaming snake case convention, e.g. EXAMPLE_FIELD_NAME.
    /// </summary>
    ScreamingSnakeCase,

    /// <summary>
    /// Represents the kebab case convention, e.g. example-field-name.
    /// </summary>
    KebabCase
}
