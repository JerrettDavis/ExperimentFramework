Feature: Governance Approvals
  As a dashboard user
  I want to understand the approval workflow
  So that I know how experiment approvals work

  Background:
    Given I am logged in as "Admin"

  Scenario: Approvals page displays workflow information
    When I navigate to "/dashboard/governance/approvals"
    Then I should see the approval workflow steps
    And I should see feature cards for approval types

  Scenario: Navigation links go to correct governance pages
    When I navigate to "/dashboard/governance/approvals"
    Then I should see links to other governance pages
