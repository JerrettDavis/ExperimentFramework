Feature: Governance Policies
  As an experiment administrator
  I want to view policy compliance
  So that I can ensure experiments meet governance requirements

  Background:
    Given I am logged in as "Admin"
    And I am on the governance policies page

  Scenario: Policies page shows compliance summary
    When I select the first experiment from the dropdown
    Then I should see the compliance summary or not configured message

  Scenario: Policy cards display compliance status
    When I select the first experiment from the dropdown
    Then each policy card should show compliant or non-compliant status
