Feature: Governance Audit Trail
  As an experiment administrator
  I want to view the governance audit trail
  So that I can track all governance actions

  Background:
    Given I am logged in as "Admin"
    And I am on the governance audit page

  Scenario: Audit page loads for selected experiment
    When I select the first experiment from the dropdown
    Then I should see audit trail entries or a not configured message

  Scenario: Filter audit entries by type
    When I select the first experiment from the dropdown
    And I filter by type "StateTransition"
    Then only state transition entries should be shown

  Scenario: Search audit entries
    When I select the first experiment from the dropdown
    And I search audit entries for "transition"
    Then the audit entries should be filtered by search text
