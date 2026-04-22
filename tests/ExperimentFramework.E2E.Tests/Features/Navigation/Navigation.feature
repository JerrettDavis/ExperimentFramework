Feature: Navigation
  As an authenticated dashboard user
  I want to navigate between all dashboard pages
  So that I can access all experiment management features

  Background:
    Given I am logged in as "Admin"

  Scenario: Dashboard home page loads with feature cards
    Given I am on the dashboard home page
    Then I should see feature cards for all sections
      | Card Title              |
      | Experiment Management   |
      | Advanced Analytics      |
      | Governance & Lifecycle  |
      | Targeting & Rollout     |
      | Plugin System           |
      | Hypothesis Testing      |

  Scenario Outline: NavMenu navigates to correct page
    Given I am on the dashboard home page
    When I click "<NavItem>" in the navigation menu
    Then I should be on the "<ExpectedPath>" page

    Examples:
      | NavItem            | ExpectedPath                    |
      | Experiments        | /dashboard/experiments          |
      | Analytics          | /dashboard/analytics            |
      | Governance         | /dashboard/governance/lifecycle |
      | Targeting Rules    | /dashboard/targeting            |
      | Rollout            | /dashboard/rollout              |
      | Hypothesis Testing | /dashboard/hypothesis           |
      | Plugins            | /dashboard/plugins              |
      | Configuration      | /dashboard/configuration        |
      | Overview           | /dashboard                      |

  Scenario Outline: Home page feature cards navigate correctly
    Given I am on the dashboard home page
    When I click the "<CardTitle>" feature card
    Then I should be on the "<ExpectedPath>" page

    Examples:
      | CardTitle               | ExpectedPath                    |
      | Experiment Management   | /dashboard/experiments          |
      | Advanced Analytics      | /dashboard/analytics            |
      | Governance & Lifecycle  | /dashboard/governance/lifecycle |
      | Targeting & Rollout     | /dashboard/targeting            |
      | Plugin System           | /dashboard/plugins              |
      | Hypothesis Testing      | /dashboard/hypothesis           |

  Scenario: DSL Editor is accessible at correct route
    When I navigate to "/dashboard/dsl"
    Then the page should load successfully
