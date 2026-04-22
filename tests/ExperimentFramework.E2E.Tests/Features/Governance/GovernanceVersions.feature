Feature: Governance Versions
  As an experiment administrator
  I want to manage experiment configuration versions
  So that I can track and rollback changes

  Background:
    Given I am logged in as "Admin"
    And I am on the governance versions page

  Scenario: Versions page shows version history
    When I select the first experiment from the dropdown
    Then I should see the version history list

  Scenario: View version details
    When I select the first experiment from the dropdown
    And I click view on the first version
    Then I should see the version detail modal with JSON content

  Scenario: Close version viewer
    When I select the first experiment from the dropdown
    And I click view on the first version
    And I close the version viewer
    Then the version viewer should be hidden
