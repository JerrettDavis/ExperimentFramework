Feature: Plugin Management
  As an experiment administrator
  I want to manage framework plugins
  So that I can extend experiment capabilities

  Background:
    Given I am logged in as "Admin"
    And I am on the plugins page

  Scenario: Plugins page loads with stats
    Then I should see the plugin stats row

  Scenario: Plugin list displays loaded plugins
    Then I should see plugin cards or an empty state

  Scenario: Refresh plugins list
    When I click the refresh button
    Then the plugin data should reload

  Scenario: Plugin cards show service information
    Given there are loaded plugins
    Then each plugin card should display its name and version
    And each plugin card should show available services
