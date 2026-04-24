Feature: Authentication
  As a user of the AspireDemo application
  I need to authenticate before accessing protected pages
  So that experiment data is secured

  Scenario: Unauthenticated user navigating to dashboard is redirected to login
    Given I am not logged in
    When I navigate to "/dashboard"
    Then I should be redirected to the login page

  Scenario: Admin can log in successfully
    Given I am on the login page
    When I log in as "Admin"
    Then I should be on the dashboard home page

  Scenario: Experimenter can log in successfully
    Given I am on the login page
    When I log in as "Experimenter"
    Then I should be on the dashboard home page

  Scenario: Viewer can log in successfully
    Given I am on the login page
    When I log in as "Viewer"
    Then I should be on the dashboard home page

  Scenario: Analyst can log in successfully
    Given I am on the login page
    When I log in as "Analyst"
    Then I should be on the dashboard home page

  Scenario: Invalid credentials show an error message
    Given I am on the login page
    When I log in with email "wrong@example.com" and password "WrongPassword1!"
    Then I should see a login error message

  @admin
  Scenario: Authenticated user can navigate to the experiments page
    When I navigate to "/dashboard/experiments"
    Then the page should load without errors
