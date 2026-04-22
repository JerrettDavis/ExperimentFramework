@docs-screenshot @screenshot-area:experiments
Feature: Docs Probe Screenshot
  A minimal smoke test to verify the screenshot infrastructure works end-to-end.

Scenario: Probe — capture experiments list
  Given I am logged in as "Admin"
  When I navigate to "/dashboard/experiments"
  And I capture screenshot "experiments-list-probe"
  Then I should see "Experiments"
