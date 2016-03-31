@driver_api @reset_database
Feature: Tests the uniform API of the driver

  1 Access Basic Result Metadata

  For a plethora of tools, statements being run are coming from somewhere else, meaning access to metadata about what a
  statement does is vital for tooling to understand it. For many other use cases as well, access to metadata about what
  a statement did or will do is very useful. Compliant drivers expose this information as a Result Summary.
  The Result Summary cannot be accessed before the full Result of the statement summarized has been retrieved from Neo4j.
  This is the case for obvious reasons - many statements execute lazily, and a complete summary is not possible to
  generate until the full result is consumed.

  1.1 Statement type

  An application may want to treat statements differently depending on their type. For instance, an updating statement
  may be disallowed for a guest application user. A compliant driver will expose the statementtype as a part of the
  Result Summary.

  1.2 Update statistics

  Neo4j exposes Update Statistics containing basic counters tracking the number of things changed by a statement.
  A compliant driver will expose update statistics as part of the Result Summary.


  2 Result Plans and Profile

  To help a user in understanding the performance characteristics of executed statements, many tools need to introspect
  the statement plan and also access profiling information. This DIP proposes adding this information to the result
  summary API.

  2.1 Plans and profiles

  If the user requests a Plan (EXPLAIN clause) or a Profile (PROFILE clause) for a statement, it is recorded by Cypher.
  The Plan or the Profile should be made available via the Result Summary by a compliant driver.

  2.2 Plans

  Plans are nodes in a plan tree structure. For each Plan, Cypher records a name, a map of arguments, the set of
  identifiers influenced by executing this part of the plan and all existing child nodes in the plan tree.
  This information should be made available for each Plan by a compliant driver. The root Plan should be made
  available via the Result Summary by a compliant driver.

  2.3 Profiled Plans

  If profiling a statement, additional information is available for each Profiled Plan, namely the number of db hits
  caused by executing this part of the plan and the number of rows processed by executing this part of the plan.
  This additional information should be made available for each Profiled Plan by a compliant driver. The root
  Profiled Plan should be made available via the Result Summary by a compliant driver.

  3 Notifications

  Notifications provide extra information for a user executing a statement. They can be warnings about problematic
  queries or other valuable information that can be presented in a client. Unlike errors, notifications do not affect
  the execution of a statement.

  3.1. Notifications and Status Codes

  Notifications are a type of [Neo4j Status Code](http://neo4j.com/docs/stable/status-codes.html), specifically,
  notifications have the classification ClientNotification. Any statement may include Notifications as part of its
  result, and compliant drivers are expected to give the user access to them.

  Unlike failures, notifications are expected to be non-intrusive, and should not interrupt program flow.

  1.2. Input Position

  Some, but not all, notifications include a specific position in the statement that generated the notification.
  This is to help users pinpoint exactly what part of their statement yielded the notification. The input position is
  optional, and consists of a three integer positions:

  - the offset from the start of the string, starting from 0

  - the line number, starting from 1

  - the column number, starting from 1


  Scenario: Summarize `Result Cursor`
    Given running: CREATE (n)
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then the `Statement Result` is closed

  Scenario: Access Statement
    Given running: CREATE (:label {name: "Pelle"} )
    When the `Statement Result` is consumed a `Result Summary` is returned
    And I request a `Statement` from the `Result Summary`
    Then requesting the `Statement` as text should give: CREATE (:label {name: "Pelle"} )
    And requesting the `Statement` parameter should give: {}

  Scenario: Access Statement parametrised
    Given running parametrized: CREATE (:label {name: {param}} )
      | param   |
      | "Pelle" |
    When the `Statement Result` is consumed a `Result Summary` is returned
    And I request a `Statement` from the `Result Summary`
    Then requesting the `Statement` as text should give: CREATE (:label {name: {param}} )
    And requesting the `Statement` parameter should give: {"param":"Pelle"}

  Scenario: Access Update Statistics and check create node, properties and relationships
    Given running: CREATE (:label1 {name: "Pelle"})<-[:T1]-(:label2 {name: "Elin"})-[:T2]->(:label3)
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then requesting `Counters` from `Result Summary` should give
      | counter               | result |
      | nodes created         | 3      |
      | nodes deleted         | 0      |
      | relationships created | 2      |
      | relationships deleted | 0      |
      | properties set        | 2      |
      | labels added          | 3      |
      | labels removed        | 0      |
      | indexes added         | 0      |
      | indexes removed       | 0      |
      | constraints added     | 0      |
      | constraints removed   | 0      |
      | contains updates      | true   |

  Scenario: Access Update Statistics and check delete node and relationship
    Given init: CREATE (:label1 {name: "Pelle"})<-[:T1]-(:label2 {name: "Elin"})-[:T2]->(:label3)
    Given running: MATCH (n:label1 {name: "Pelle"})<-[r:T1]-(:label2 {name: "Elin"})-[:T2]->(:label3) DELETE n,r
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then requesting `Counters` from `Result Summary` should give
      | counter               | result |
      | nodes created         | 0      |
      | nodes deleted         | 1      |
      | relationships created | 0      |
      | relationships deleted | 1      |
      | properties set        | 0      |
      | labels added          | 0      |
      | labels removed        | 0      |
      | indexes added         | 0      |
      | indexes removed       | 0      |
      | constraints added     | 0      |
      | constraints removed   | 0      |
      | contains updates      | true   |

  Scenario: Access Update Statistics and check create index
    Given running: CREATE INDEX on :Label(prop)
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then requesting `Counters` from `Result Summary` should give
      | counter               | result |
      | nodes created         | 0      |
      | nodes deleted         | 0      |
      | relationships created | 0      |
      | relationships deleted | 0      |
      | properties set        | 0      |
      | labels added          | 0      |
      | labels removed        | 0      |
      | indexes added         | 1      |
      | indexes removed       | 0      |
      | constraints added     | 0      |
      | constraints removed   | 0      |
      | contains updates      | true   |

  Scenario: Access Update Statistics and check delete index
    Given running: DROP INDEX on :Label(prop)
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then requesting `Counters` from `Result Summary` should give
      | counter               | result |
      | nodes created         | 0      |
      | nodes deleted         | 0      |
      | relationships created | 0      |
      | relationships deleted | 0      |
      | properties set        | 0      |
      | labels added          | 0      |
      | labels removed        | 0      |
      | indexes added         | 0      |
      | indexes removed       | 1      |
      | constraints added     | 0      |
      | constraints removed   | 0      |
      | contains updates      | true   |

  Scenario: Access Update Statistics and check create constraint
    Given running: CREATE CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then requesting `Counters` from `Result Summary` should give
      | counter               | result |
      | nodes created         | 0      |
      | nodes deleted         | 0      |
      | relationships created | 0      |
      | relationships deleted | 0      |
      | properties set        | 0      |
      | labels added          | 0      |
      | labels removed        | 0      |
      | indexes added         | 0      |
      | indexes removed       | 0      |
      | constraints added     | 1      |
      | constraints removed   | 0      |
      | contains updates      | true   |

  Scenario: Access Update Statistics and check delete constraint
    Given running: DROP CONSTRAINT ON (book:Book) ASSERT book.isbn IS UNIQUE
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then requesting `Counters` from `Result Summary` should give
      | counter               | result |
      | nodes created         | 0      |
      | nodes deleted         | 0      |
      | relationships created | 0      |
      | relationships deleted | 0      |
      | properties set        | 0      |
      | labels added          | 0      |
      | labels removed        | 0      |
      | indexes added         | 0      |
      | indexes removed       | 0      |
      | constraints added     | 0      |
      | constraints removed   | 1      |
      | contains updates      | true   |

  Scenario Outline: Access Statement Type
    Given running: <statement>
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then requesting the `Statement Type` should give <type>

    Examples:
      | type         | statement                            |
      | read only    | RETURN 1                             |
      | read write   | CREATE (n) WITH * MATCH (n) RETURN n |
      | write only   | CREATE (n)                           |
      | schema write | CREATE INDEX on :Label(prop)         |
      | schema write | DROP INDEX on :Label(prop)           |

  Scenario: Check that plan and no profile is available
    Given running: EXPLAIN CREATE (n) RETURN n
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then the `Result Summary` has a `Plan`
    And the `Result Summary` does not have a `Profile`
    And requesting the `Plan` it contains
      | plan method   | result           |
      | operator type | ProduceResults   |
    And the `Plan` also contains method calls for:
      | plan method | type                  |
	  | identifiers | ["n"]                 |
      | children    | list of plans         |
      | arguments   | map of string, values |

  Scenario: Check that no plan or no profile is available
    Given running: CREATE (n) RETURN n
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then the `Result Summary` does not have a `Plan`
    And the `Result Summary` does not have a `Profile`

  Scenario: Check that plan and profile is available
    Given running: PROFILE CREATE (n) RETURN n
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then the `Result Summary` has a `Profile`
    And the `Result Summary` has a `Plan`
    And requesting the `Profile` it contains:
      | plan method   | result           |
      | operator type | ProduceResults   |
      | db hits       | 0                |
      | records       | 1                |
    And the `Profile` also contains method calls for:
      | plan method | type                  |
	  | identifiers | ["n"]                 |
      | children    | list of profiled plans|
      | arguments   | map of string, values |


  Scenario: Check that no notification is available
    Given running: CREATE (n) RETURN n
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then the `Result Summary` `Notifications` is empty


  Scenario: Check that notifications are available
    Given running: EXPLAIN MATCH (n),(m) RETURN n,m
    When the `Statement Result` is consumed a `Result Summary` is returned
    Then the `Result Summary` `Notifications` has one notification with
      | key         | value                                                                                                                                                                                                                                                                                                                                                                                                                                       |
      | code        | "Neo.ClientNotification.Statement.CartesianProductWarning"                                                                                                                                                                                                                                                                                                                                                                                  |
      | title       | "This query builds a cartesian product between disconnected patterns."                                                                                                                                                                                                                                                                                                                                                                      |
      | severity    | "WARNING"                                                                                                                                                                                                                                                                                                                                                                                                                                   |
      | description | "If a part of a query contains multiple disconnected patterns, this will build a cartesian product between all those parts. This may produce a large amount of data and slow down query processing. While occasionally intended, it may often be possible to reformulate the query that avoids the use of this cross product, perhaps by adding a relationship between the different parts or by using OPTIONAL MATCH (identifier is: (m))" |
      | position    | {"offset": 0,"line": 1,"column": 1}                                                                                                                                                                                                                                                                                                                                                                                                         |