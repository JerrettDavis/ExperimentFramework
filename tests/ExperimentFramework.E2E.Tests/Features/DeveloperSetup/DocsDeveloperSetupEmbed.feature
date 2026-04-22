@docs-screenshot @screenshot-area:developer-setup
Feature: Docs Developer Setup — Embed the Dashboard
  Captures the two screenshots for the embed.md developer setup page.

Scenario: Developer Setup — embed minimal and seeded
  Given I am logged in as "Admin"
  When I navigate to "/dashboard"
  Then I should see "Experiment Dashboard"
  When I capture screenshot "dashboard-first-load"
  When I navigate to "/dashboard/experiments"
  And I wait for the experiments list to load
  And I capture screenshot "dashboard-with-experiment"
  Then I should see "Experiments"
