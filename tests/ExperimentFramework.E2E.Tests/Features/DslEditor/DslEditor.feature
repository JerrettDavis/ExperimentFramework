Feature: DSL Editor
  As an experiment administrator
  I want to write and validate experiment definitions in YAML
  So that I can configure experiments using the DSL

  Background:
    Given I am logged in as "Admin"
    And I am on the DSL editor page

  Scenario: DSL editor loads with Monaco editor
    Then the Monaco editor should be initialized
    And the editor should contain YAML content

  Scenario: Load current configuration
    When I click the load current button
    Then the editor content should be updated

  Scenario: Validate valid YAML
    Given the editor contains valid experiment YAML
    When I click the validate button
    Then the validation status should show "Valid"

  Scenario: Validate invalid YAML
    Given the editor contains invalid YAML "invalid: [yaml: broken"
    When I click the validate button
    Then I should see validation errors

  Scenario: Apply changes shows confirmation dialog
    Given the editor contains valid experiment YAML
    And the YAML has been validated successfully
    When I click the apply changes button
    Then I should see the apply confirmation modal

  Scenario: Cancel apply changes
    Given the editor contains valid experiment YAML
    And the YAML has been validated successfully
    When I click the apply changes button
    And I cancel the apply
    Then the confirmation modal should close

  Scenario: Clear validation results
    Given validation results are shown
    When I click the clear button
    Then the validation results should be cleared
