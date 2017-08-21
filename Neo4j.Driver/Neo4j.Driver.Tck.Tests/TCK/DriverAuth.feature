@auth
Feature: Authentication for drivers

  Scenario: Should be able to start and run against database with driver auth enabled and correct password is provided
    Given a driver is configured with auth enabled and correct password is provided
    Then reading and writing to the database should be possible

  Scenario: Should not be able to start and run against database with driver auth enabled and wrong password is provided
    Given a driver is configured with auth enabled and the wrong password is provided
    Then reading and writing to the database should not be possible
    And a `Protocol Error` is raised