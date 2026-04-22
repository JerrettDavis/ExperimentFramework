Feature: Configuration View
  As a dashboard user
  I want to view the framework configuration
  So that I can understand the current setup

  Background:
    Given I am logged in as "Admin"
    And I am on the configuration page

  Scenario: Configuration page shows framework info
    Then I should see the framework info cards
    And the framework name should not be empty
    And the runtime version should be displayed

  Scenario: Configuration page shows YAML export
    Then I should see the YAML configuration block
    And the YAML content should not be empty

  Scenario: Copy YAML to clipboard
    When I click the copy to clipboard button
    Then the button should show a copied confirmation

  Scenario: Enabled features are displayed
    Then I should see the enabled features section

  Scenario: Refresh configuration
    When I click the refresh button
    Then the configuration data should reload
