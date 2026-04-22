Feature: Hypothesis Testing Dashboard
  As an experiment analyst
  I want to view hypothesis test results
  So that I can make data-driven decisions

  Background:
    Given I am logged in as "Analyst"
    And I am on the hypothesis testing page

  Scenario: Hypothesis page loads and seeds demo data
    Then I should see hypothesis cards or an empty state

  Scenario: Hypothesis cards show test status
    Given there are experiments with hypotheses
    Then each hypothesis card should show a status badge
    And the status should be one of "running", "completed", or "draft"

  Scenario: Hypothesis cards display test design
    Given there are experiments with hypotheses
    Then each hypothesis card should display the test type
    And each hypothesis card should display the primary metric

  Scenario: Hypothesis results show statistical data
    Given there are completed hypothesis tests
    Then I should see sample sizes for control and treatment
    And I should see the effect size
    And I should see the p-value
    And I should see the confidence interval

  Scenario: Refresh hypothesis data
    When I click the refresh button
    Then the hypothesis data should reload
