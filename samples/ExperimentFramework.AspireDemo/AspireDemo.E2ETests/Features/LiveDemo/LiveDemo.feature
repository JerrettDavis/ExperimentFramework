Feature: Live Demo
  As an authenticated user of the AspireDemo web frontend
  I want to interact with the Live Test Lab page (/demo)
  So that I can observe experiment variants being switched in real time

  # /demo is a Blazor page rendered under the AspireDemo.Web frontend.
  # It calls the ApiService for pricing, notifications, recommendations, and theme.
  # Authentication is required to view the dashboard layout; /demo shares the MainLayout.

  @authenticated
  Scenario: Live Demo page loads with the experiment demo cards
    When I navigate to "/demo"
    Then the live demo heading should be visible
    And the pricing calculator card should be visible
    And the notification preview card should be visible
    And the recommendations card should be visible

  @authenticated
  Scenario: Welcome page loads and shows navigation cards
    When I navigate to "/welcome"
    Then the welcome page heading should contain "AspireDemo"
    And the Experiment Dashboard link should be visible
    And the Live Test Lab link should be visible
