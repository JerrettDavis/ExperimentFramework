Feature: Progressive Rollout
  As an experiment administrator
  I want to configure progressive rollouts
  So that I can gradually release experiments to users

  Background:
    Given I am logged in as "Admin"
    And I am on the rollout page

  Scenario: Rollout page loads with experiment selector
    Then I should see the experiment selector dropdown

  Scenario: Select an experiment to configure rollout
    When I select the first experiment from the dropdown
    Then I should see the rollout configuration panel

  Scenario: Add rollout stages
    When I select the first experiment from the dropdown
    And I add a stage with name "Canary" percentage 10 and duration 2
    And I add a stage with name "Beta" percentage 50 and duration 4
    And I add a stage with name "GA" percentage 100 and duration 0
    Then I should see 3 stages configured

  Scenario: Remove a rollout stage
    When I select the first experiment from the dropdown
    And I add a stage with name "Test" percentage 25 and duration 1
    And I remove the last stage
    Then the stage count should decrease by 1

  @admin
  Scenario: Start a rollout
    When I select the first experiment from the dropdown
    And I select a target variant
    And I add a stage with name "Canary" percentage 10 and duration 2
    And I add a stage with name "GA" percentage 100 and duration 0
    And I start the rollout
    Then the rollout should be in progress
    And I should see the progress bar
