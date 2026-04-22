Feature: Experiments Management
  As an experiment administrator
  I want to view and manage experiments
  So that I can control A/B test configurations

  Background:
    Given I am logged in as "Admin"
    And I am on the experiments page

  Scenario: Experiments page loads with stats
    Then I should see the experiments stats row
    And the total experiments count should be greater than 0

  Scenario: Experiments list shows all experiments
    Then I should see a list of experiments
    And each experiment should display its name and status

  Scenario: Filter experiments by category
    When I filter experiments by category "Revenue"
    Then only experiments in the "Revenue" category should be shown
    When I filter experiments by category "All"
    Then all experiments should be shown

  Scenario: Search experiments by name
    When I search for "pricing"
    Then the experiment list should be filtered to matching results
    When I clear the search
    Then all experiments should be shown

  Scenario: Expand and collapse experiment details
    When I expand the first experiment
    Then I should see the experiment details panel
    And I should see variant cards
    When I collapse the first experiment
    Then the experiment details panel should be hidden

  @admin
  Scenario: Toggle kill switch on an experiment
    When I expand the first experiment
    And I toggle the kill switch
    Then the kill switch state should change

  @admin
  Scenario: Activate a variant
    When I expand the first experiment
    And I click on a variant card
    Then the variant should be marked as active

  Scenario: Skeleton loading state appears while loading
    Given I am on a slow connection
    When I navigate to the experiments page
    Then I should see skeleton loading placeholders
