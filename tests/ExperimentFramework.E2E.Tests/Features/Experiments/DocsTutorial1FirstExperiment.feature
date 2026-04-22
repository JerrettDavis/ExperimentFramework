@docs-screenshot @screenshot-area:experiments
Feature: Docs Tutorial 1 — Your First Experiment
  Captures all screenshots for the "Your first experiment" tutorial page.
  Uses the checkout-button-v2 seeded experiment as the worked example.

Scenario: Tutorial 1 — full walkthrough screenshots
  Given I am logged in as "Admin"
  When I navigate to "/dashboard/experiments"
  And I wait for the experiments list to load
  And I capture screenshot "first-exp-experiments-overview"
  When I expand the experiment named "checkout-button-v2"
  And I capture screenshot "first-exp-experiment-detail"
  And I capture screenshot "first-exp-arms-detail"
  When I collapse the experiment named "checkout-button-v2"
  And I capture screenshot "first-exp-list-annotated"
  When I expand the experiment named "checkout-button-v2"
  And I capture screenshot "first-exp-toggle-before"
  When I toggle the killswitch for "checkout-button-v2"
  And I capture screenshot "first-exp-toggle-after"
  Then I should see "checkout-button-v2"
