Feature: Governance Lifecycle Management
  As an experiment administrator
  I want to manage experiment lifecycle states
  So that experiments follow proper governance workflows

  Background:
    Given I am logged in as "Admin"
    And I am on the governance lifecycle page

  Scenario: Governance page shows not-configured message when persistence is missing
    Then I should see the governance not configured message

  Scenario: Select an experiment to view its governance state
    When I select the first experiment from the dropdown
    Then I should see the current governance state
    And I should see available transitions

  Scenario: View transition history
    When I select the first experiment from the dropdown
    Then I should see the transition history section

  @admin
  Scenario: Perform a state transition
    When I select the first experiment from the dropdown
    And I click the first available transition
    And I fill in the transition form with actor "admin" and reason "Testing transition"
    And I confirm the transition
    Then the governance state should update
