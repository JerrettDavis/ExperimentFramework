Feature: Blog
  As a visitor to the AspireDemo TechBlog
  I want to browse posts and view post detail pages
  So that I can read content powered by the ExperimentFramework plugin system

  # The Blog is a separate Blazor app at https://localhost:7120.
  # It is publicly accessible (no authentication required).

  Scenario: Blog home page loads and shows the hero section
    When I navigate to the blog home page
    Then the blog hero section should be visible with title "TechBlog"

  Scenario: Blog home page displays the active plugin indicators
    When I navigate to the blog home page
    Then the active plugin indicators should be visible

  Scenario: Blog home page shows the posts grid or empty state
    When I navigate to the blog home page
    Then the posts section should be visible

  Scenario: Blog home page shows the categories sidebar
    When I navigate to the blog home page
    Then the categories sidebar should be visible

  Scenario: Blog home page shows the authors sidebar
    When I navigate to the blog home page
    Then the authors sidebar should be visible

  Scenario: Navigating to a non-existent post slug shows a not-found state
    When I navigate to the blog post page with slug "this-post-does-not-exist"
    Then the post not-found message should be visible

  Scenario: Blog admin page loads and shows statistics
    When I navigate to the blog admin page
    Then the blog administration heading should be visible

  Scenario: Blog authors page loads
    When I navigate to the blog authors page
    Then the authors heading should be visible

  Scenario: Blog categories page loads
    When I navigate to the blog categories page
    Then the categories heading should be visible
