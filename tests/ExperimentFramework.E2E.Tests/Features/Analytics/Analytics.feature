Feature: Analytics Dashboard
  As an experiment analyst
  I want to view experiment analytics
  So that I can understand experiment performance

  Background:
    Given I am logged in as "Analyst"
    And I am on the analytics page

  Scenario: Analytics page loads with stats
    Then I should see the analytics stats row
    And I should see tracked experiments count
    And I should see total selections count

  Scenario: Audit log displays entries
    Then I should see the audit log table
    And the audit log should have entries with timestamps

  Scenario: Refresh analytics data
    When I click the refresh button
    Then the analytics data should reload

  Scenario: Variant distribution is displayed
    Then I should see variant distribution charts
