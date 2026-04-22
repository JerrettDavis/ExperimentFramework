Feature: Targeting Rules
  As a dashboard user
  I want to view targeting rules for experiments
  So that I can understand how users are segmented

  Background:
    Given I am logged in as "Admin"
    And I am on the targeting rules page

  Scenario: Targeting page loads with experiment rules
    Then I should see the targeting rules display
    And all toggle switches should be disabled

  Scenario: Targeting rules show conditions and variants
    Then each targeting rule should display condition tags
    And each targeting rule should display a target variant

  Scenario: Refresh targeting rules
    When I click the refresh button
    Then the targeting rules should reload
