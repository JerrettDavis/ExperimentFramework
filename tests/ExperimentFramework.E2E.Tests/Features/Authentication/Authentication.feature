Feature: Authentication
  As a user of the ExperimentFramework Dashboard
  I need to authenticate before accessing the dashboard
  So that experiment data is protected

  Scenario: Unauthenticated user is redirected to login
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

  Scenario: Invalid credentials show error message
    Given I am on the login page
    When I log in with email "wrong@example.com" and password "WrongPassword1!"
    Then I should see an error message "Invalid email or password."

  Scenario: User can log out
    Given I am logged in as "Admin"
    When I log out
    Then I should be on the login page

  @admin
  Scenario: Remember me checkbox is available
    Given I am on the login page
    Then the remember me checkbox should be visible
