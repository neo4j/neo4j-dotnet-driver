@equality_test @reset_database
Feature: Driver Equals Feature

  Comparing Nodes, Relationship and Path should not bother with the content of those objects but rather compare if these
  are the same object.

  Nodes and Relationships

  Nodes and Relationships should compare IDs only. This means that the content may differ since it may mean comparing
  these at different times.

  Paths
  Paths need to compare the IDs of the Nodes and Relationships inside the path as well as the start and end values of
  the relationships. A path is only equal if it is the same path containing the same elements. 

  Scenario: Compare modified node
    Given init: CREATE (:label1)
    And `value1` is single value result of: MATCH (n:label1) RETURN n
    When running: MATCH (n:label1) SET n.foo = 'bar' SET n :label2
    And `value2` is single value result of: MATCH (n:label1) RETURN n
    Then saved values should all equal

  Scenario: Compare different nodes with same content
    Given `value1` is single value result of: CREATE (n:label1) RETURN n
    And `value2` is single value result of:  CREATE (n:label1) RETURN n
    Then none of the saved values should be equal

  Scenario: Compare modified Relationship
    Given init: CREATE (a {name: "A"}), (b {name: "B"}), (a)-[:KNOWS]->(b)
    And `value1` is single value result of: MATCH (n)-[r:KNOWS]->(x) RETURN r
    When running: MATCH (n)-[r:KNOWS]->(x) SET r.foo = 'bar'
    And `value2` is single value result of: MATCH (n)-[r:KNOWS]->(x) RETURN r
    Then saved values should all equal

  Scenario: Compare different relationships with same content
    Given `value1` is single value result of: CREATE (a {name: "A"}), (b {name: "B"}), (a)-[r:KNOWS]->(b) return r
    And `value2` is single value result of:  CREATE (a {name: "A"}), (b {name: "B"}), (a)-[r:KNOWS]->(b) return r
    Then none of the saved values should be equal

  Scenario: Compare modified path
    Given init: CREATE (a:A {name: "A"})-[:KNOWS]->(b:B {name: "B"})
    And `value1` is single value result of: MATCH p=(a {name:'A'})-->(b) RETURN p
    When running: MATCH (n:A {name: "A"}) SET n.foo = 'bar' SET n :label2
    And running: MATCH (n)-[r:KNOWS]->(x) SET r.foo = 'bar'
    And `value2` is single value result of: MATCH p=(a {name:'A'})-->(b) RETURN p
    Then saved values should all equal

  Scenario: Compare different path with same content
    Given `value1` is single value result of: CREATE p=((a:A {name: "A"})-[:KNOWS]->(b:B {name: "B"})) RETURN p
    And `value2` is single value result of: CREATE p=((a:A {name: "A"})-[:KNOWS]->(b:B {name: "B"})) RETURN p
    Then none of the saved values should be equal