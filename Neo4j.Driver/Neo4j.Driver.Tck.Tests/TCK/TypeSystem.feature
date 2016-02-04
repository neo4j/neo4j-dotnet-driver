@type_test
Feature: Driver Types Test Echoing Single Parameter
  The following types are supported by bolt.
  | Null        | Represents the absence of a value   |
  | Boolean     | Boolean true or false |
  | Integer     | 64-bit signed integer |
  | Float       | 64-bit floating point number|
  | String      | Unicode string|
  | List        | Ordered collection of values|
  | Map         | Unordered, keyed collection of values|
  | Node        | A node in the graph with optional properties and labels|
  | Relationship| A directed, typed connection between two nodes. Each relationship may have properties and always has an identity|
  | Path        | The record of a directed walk through the graph, a sequence of zero or more segments*. A path with zero segments consists of a single node.|

  It is important that the types that are sent over Bolt are not corrupted.
  These Scenarios will echo different types and make sure that the returned object is of the same type and value as
  the one sent to the server.

  Echoing to the server can be done by using the cypher statement "RETURN <value>",
  or "RETURN {value}" with value provided via a parameter.
  It is recommended to test each supported way of sending statements that the driver provides while running these
  cucumber scenarios.

  Background:
    Given A running database

  Scenario Outline: should return the same type and value
    Given a value <Input> of type <BoltType>
    When the driver asks the server to echo this value back
    And the value given in the result should be the same as what was sent

    Examples: Null
      | BoltType | Input |
      | Null     | null  |

    Examples: Boolean
      | BoltType | Input |
      | Boolean  | true  |
      | Boolean  | false |

    Examples: Integer
      #  Byte = [-2^4, 2^7)
      #  INT_8 = [-2^7, -2^4)
      #  INT_16 = [-2^15, 2^15)
      #  INT_32 = [-2^31, 2^31)
      #  INT_64 = [-2^63, 2^63)
      | BoltType | Input                |
      | Integer  | 1                    |
      | Integer  | -17                  |
      | Integer  | -129                 |
      | Integer  | 129                  |
      | Integer  | 2147483647           |
      | Integer  | -2147483648          |
      | Integer  | 9223372036854775807  |
      | Integer  | -9223372036854775808 |

    Examples: Float
      | BoltType | Input                   |
      | Float    | 1.7976931348623157E+308 |
      | Float    | 2.2250738585072014e-308 |
      | Float    | 4.9E-324                |
      | Float    | 0                       |
      | Float    | 1.1                     |

    Examples: String
      | BoltType | Input   |
      | String   | 1       |
      | String   | -17∂ßå® |
      | String   | String  |
      | String   |         |


  Scenario Outline: Should echo list
    Given a list value <Input> of type <BoltType>
    When the driver asks the server to echo this value back
    And the value given in the result should be the same as what was sent
    Examples:
      | BoltType | Input         |
      | Integer  | [1,2,3,4]     |
      | Boolean  | [true,false]  |
      | Float    | [1.1,2.2,3.3] |
      | String   | [a,b,c,˚C]    |
      | Null     | [null, null]  |

  Scenario: Should echo list of lists, maps and values
    Given an empty list L
    And adding a table of lists to the list L
      | Integer | [1,2,3,4]     |
      | Boolean | [true,true]   |
      | Float   | [1.1,2.2,3.3] |
      | String  | [a,b,c,˚C]    |
      | Null    | [null,null]   |
    And adding a table of values to the list L
      | Integer | 1    |
      | Boolean | true |
      | Float   | 1.1  |
      | String  | ˚C   |
      | Null    | null |
    And an empty map M
    And adding a table of values to the map M
      | Integer | 1    |
      | Boolean | true |
      | Float   | 1.1  |
      | String  | ˚C   |
      | Null    | null |
    And adding map M to list L
    When the driver asks the server to echo this list back
    And the value given in the result should be the same as what was sent

  Scenario: Should echo map
    Given an empty map M
    When adding a table of lists to the map M
      | Integer | [1,2,3,4]     |
      | Boolean | [true,true]   |
      | Float   | [1.1,2.2,3.3] |
      | String  | [a,b,c,˚C]    |
      | Null    | [null,null]   |
    And adding a table of values to the map M
      | Integer | 1    |
      | Boolean | true |
      | Float   | 1.1  |
      | String  | ˚C   |
      | Null    | null |
    And adding a copy of map M to map M
    When the driver asks the server to echo this map back
    And the value given in the result should be the same as what was sent
