# Phase 5 — Deterministic Script Generator

## Overview
Phase 5 implements the core **Script Generation Pipeline** that generates runnable test code from test scenarios using Scriban templates. This phase establishes the deterministic code generation foundation upon which AI-based generation (Phase 6) will be built.

## Completed Features

### 1. Domain Models

#### ScriptOutput
**Purpose**: Represents generated test script output
- `Framework`: Target framework identifier (e.g., "Selenium Java")
- `PageObjectCode`: Generated Page Object class code
- `TestClassCode`: Generated Test class code
- `Success`: Boolean indicating generation status
- `ErrorMessage`: Error details if generation failed
- `GeneratedAt`: Timestamp when script was generated
- `GetCombinedOutput()`: Method to retrieve combined code

#### ScriptGenerationContext
**Purpose**: Provides all data needed for template rendering
- `Scenario`: TestScenario containing steps to generate code for
- `Framework`: Target framework selection
- `PageObjectClassName`: Custom Page Object class name
- `TestClassName`: Custom Test class name
- `PackageName`: Package/namespace for generated code
- `Elements`: List of ElementWithLocator for template substitution

#### ElementWithLocator
**Purpose**: Represents element metadata for template rendering
- `ElementId`: Unique identifier for variable naming
- `ElementType`: HTML element type (button, input, etc.)
- `Locator`: Primary locator string
- `LocatorType`: Locator strategy (css, xpath, id, etc.)
- `VariableName`: Generated variable name (e.g., submitButton_1)

### 2. IScriptGenerator Interface

**Responsibilities**:
1. **Generate Scripts**: `GenerateScriptAsync(ScriptGenerationContext)`
   - Accepts context with scenario and settings
   - Returns ScriptOutput with generated code
   - Handles errors gracefully

2. **List Frameworks**: `GetAvailableFrameworks()`
   - Returns list of supported framework names
   - Used to populate UI selector

### 3. ScriptGenerationService Implementation

**Template Loading**:
- Loads Scriban templates (currently inline, future: embedded resources)
- Supports two template types: Page Object and Test Class
- Parses templates using Scriban.Template.Parse()

**Template Variables**:
- `package_name`: Package/namespace
- `test_class_name`: Generated test class name
- `page_object_name`: Generated page object class name
- `steps`: List of test steps with descriptions
- `elements`: List of elements with locators and variable names

**Current Templates**:
- **Selenium Java Test**: JUnit-based test class with Page Object integration
- **Selenium Java Page**: Page Object Pattern class with @FindBy annotations

**Generation Process**:
1. Validates scenario has steps
2. Builds template data from context and scenario
3. Renders Page Object template
4. Renders Test Class template
5. Returns ScriptOutput with generated code or error

### 4. UI Components

#### Toolbar Enhancements
- **Framework ComboBox**: Selector for target framework
  - Populated from `IScriptGenerator.GetAvailableFrameworks()`
  - Default: "Selenium Java"
- **Generate Script Button**: Triggers code generation
  - Blue button (#2196F3) for prominence
  - Disabled during generation

#### Output Panel (Bottom Section)
**Header**:
- Title: "Generated Script Output"
- Copy Page Object button: Copies Page Object code to clipboard
- Copy Test Class button: Copies Test Class code to clipboard

**TabControl**:
- **Page Object Tab**:
  - Read-only TextBox displaying Page Object code
  - Monospace font (Courier New, 9pt)
  - Auto vertical scrolling enabled
  - Height: 200px total for output panel

- **Test Class Tab**:
  - Read-only TextBox displaying Test Class code
  - Same formatting as Page Object tab

### 5. Service Registration

**ServiceProvider.cs**:
```csharp
services.AddSingleton<IScriptGenerator, ScriptGenerationService>();
```

### 6. Integration Points

#### MainWindow Constructor
- Receives `IScriptGenerator` via dependency injection
- Stored in `_scriptGenerator` field

#### Window_Loaded
- Calls `InitializeFrameworkSelector()` to populate framework combobox
- Existing initialization routines unaffected

#### GenerateButton_Click Event Handler
1. Validates scenario exists with steps
2. Builds ScriptGenerationContext from current state
3. Calls `_scriptGenerator.GenerateScriptAsync(context)`
4. Displays output in respective TextBoxes
5. Updates status messages and error handling

#### Copy Button Handlers
- CopyPageObjectButton_Click: Copies PageObjectTextBox content to clipboard
- CopyTestClassButton_Click: Copies TestClassTextBox content to clipboard
- Both provide user feedback and error handling

#### Helper Method: BuildElementsList()
- Extracts elements from scenario steps
- Creates ElementWithLocator objects for template rendering
- Maps element types to variable names
- Uses CSS selectors as default locator type

### 7. Available Frameworks

Phase 5 supports framework selection UI for:
- Selenium Java (with templates)
- Selenium C# (placeholder)
- Playwright TypeScript (placeholder)
- Playwright .NET (placeholder)

Future phases will implement templates for all frameworks.

## Key Features Implemented

- ✅ Scriban template engine integration
- ✅ Inline template definitions (Page Object + Test Class)
- ✅ Dynamic context building from scenarios
- ✅ Code generation with variable substitution
- ✅ Framework selector UI
- ✅ Output display with copy-to-clipboard
- ✅ Comprehensive error handling
- ✅ User status feedback
- ✅ Asynchronous generation pipeline
- ✅ Extensible architecture for future frameworks

## Code Generation Pipeline

```
User Input:
- Scenario with steps
- Framework selection

↓

GenerateButton_Click:
- Validates scenario
- Builds context
- Calls GenerateScriptAsync

↓

ScriptGenerationService:
- Loads templates
- Prepares template data
- Renders with Scriban

↓

Output:
- ScriptOutput object
- Page Object code
- Test Class code
- Error details (if failed)

↓

UI Display:
- Show in respective tabs
- Enable copy buttons
- Update status
```

## Template Example (Selenium Java)

**Page Object Class**:
```java
package com.example.automation;

import org.openqa.selenium.*;
import org.openqa.selenium.support.FindBy;
import org.openqa.selenium.support.PageFactory;

public class ApplicationPage {
    private WebDriver driver;

    @FindBy(css = "button.submit")
    private WebElement submitButton;

    public ApplicationPage(WebDriver driver) {
        this.driver = driver;
        PageFactory.initElements(driver, this);
    }

    public void clickSubmitButton() {
        submitButton.click();
    }
}
```

**Test Class**:
```java
package com.example.automation;

import org.openqa.selenium.*;
import org.openqa.selenium.chrome.ChromeDriver;
import org.junit.After;
import org.junit.Before;
import org.junit.Test;
import static org.junit.Assert.*;

public class ApplicationTest {
    private WebDriver driver;
    private ApplicationPage page;

    @Before
    public void setUp() {
        System.setProperty("webdriver.chrome.driver", "./chromedriver");
        driver = new ChromeDriver();
        page = new ApplicationPage(driver);
    }

    @After
    public void tearDown() {
        if (driver != null) {
            driver.quit();
        }
    }

    @Test
    public void testScenario() {
        driver.get("https://example.com");
        
        // Click button
        // Type text in input
        
        assertTrue(true);
    }
}
```

## Build Status

✅ **Build Successful** - Zero compilation errors

```
Restore complete (0.5s)
TestScriptGeneratorTool succeeded (2.5s)
```

## File Modifications Summary

| File | Changes | Lines |
|------|---------|-------|
| Application/ScriptOutput.cs | NEW | 32 |
| Application/ScriptGenerationContext.cs | NEW | 45 |
| Application/IScriptGenerator.cs | NEW | 20 |
| Application/ScriptGenerationService.cs | NEW | 185 |
| Core/ServiceProvider.cs | MODIFIED | +1 |
| MainWindow.xaml | MODIFIED | +25 (Generate button, Framework selector, Output panel) |
| MainWindow.xaml.cs | MODIFIED | +150 (Constructor, GenerateButton_Click, copy handlers, helper methods) |

## Testing Phase 5

### Manual Test Scenario
1. Open application
2. Navigate to any website
3. Create test scenario:
   - Add "Click" action on button element
   - Add "TypeText" action on input element
4. Select framework (default: Selenium Java)
5. Click "Generate Script" button
6. Verify Page Object code appears in "Page Object" tab
7. Verify Test Class code appears in "Test Class" tab
8. Click "Copy Page Object" - should copy to clipboard
9. Click "Copy Test Class" - should copy to clipboard

### Expected Output
- Page Object class with @FindBy annotations
- Test class with proper imports and test method
- Step descriptions in comments
- Proper package/class naming

## Success Criteria Met

✅ Implement Scriban template loading
✅ Implement one Selenium Java template (Page Object + Test class)
✅ Implement ScriptOutput display in output panel
✅ Verify: Basic generated code appears in output panel
✅ Framework selection UI
✅ Copy-to-clipboard functionality
✅ Error handling and user feedback
✅ Build verification with zero errors

## Architecture Notes

**Extensibility for Future Phases**:
1. **Phase 6 (AI Generation)**: Will replace template rendering with LLM calls
2. **Phase 7 (Multi-Framework)**: Add more framework templates to `LoadTemplates()`
3. **Phase 8 (Hybrid Mode)**: Selector between deterministic and AI modes
4. **Phase 9 (Plugins)**: Plugin frameworks can register custom templates

**Current Limitations**:
- Templates are hardcoded inline (templates loaded from embedded resources in Phase 6+)
- Step descriptions in test method are comments only (Phase 6 will generate full step implementations)
- No assertion code generation (Phase 6 will use AI to infer assertions)
- Single framework template set (Phase 7 will add more frameworks)

## Next Phase Preview

**Phase 6 — AI Generation Engine** will:
- Replace template rendering with LLM prompts
- Support OpenAI and Ollama adapters
- Implement response parsing and validation
- Generate realistic step implementations from AI
- Add automatic retry and repair mechanism
