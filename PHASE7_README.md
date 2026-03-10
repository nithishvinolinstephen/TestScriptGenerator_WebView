# Phase 7: AI-Driven Test Generation with Element Selection and Step Definition

## Overview

This phase completes the core feature of the Test Script Generator: **AI-powered automated test script generation** where users can:
1. **Select elements** from a live browser
2. **Define test steps** that describe what actions to perform
3. **Generate production-ready code** for Page Object and Test classes

## Architecture

### Prompt Template System

The application now uses **framework-specific prompt templates** embedded as resources in the executable. This ensures AI generates code tailored to each automation framework.

#### Directory Structure
```
Prompts/
├── system.txt                           # Universal system prompt (all frameworks)
├── SeleniumJava/
│   ├── system.txt                       # System prompt copy for Java
│   └── user.txt                         # Java-specific user prompt template
├── SeleniumCSharp/
│   ├── system.txt                       # System prompt copy for C#
│   └── user.txt                         # C# NUnit-specific template
├── PlaywrightTypeScript/
│   ├── system.txt                       # System prompt copy for TypeScript
│   └── user.txt                         # TypeScript Jest-specific template
└── PlaywrightDotNet/
    ├── system.txt                       # System prompt copy for .NET
    └── user.txt                         # .NET Playwright-specific template
```

### System Prompt (Universal)

The `system.txt` contains 10 critical rules that apply to ALL frameworks:

1. Return ONLY code (no explanations)
2. Generate exactly TWO code blocks (Page Object first, Test class second)
3. Use Page Object Model pattern
4. Use stable locators (id > name > data-qa > css > xpath)
5. Include explicit waits (no Thread.Sleep)
6. Use actual values from metadata (no placeholders)
7. Follow framework best practices
8. Wrap code in language-specific fences (```java, ```csharp, ```typescript)
9. Include all necessary imports
10. Consistent formatting

### User Prompts (Framework-Specific)

Each framework has a user prompt template with dynamic placeholders:

- **{{ELEMENTS}}**: Formatted list of selected elements
- **{{STEPS}}**: Formatted list of test steps to automate

#### Selenium Java Template
- Framework: Selenium 4
- Language: Java
- Test Runner: JUnit 5
- Wait Strategy: WebDriverWait with ExpectedConditions

#### Selenium C# Template
- Framework: Selenium 4
- Language: C#
- Test Runner: NUnit 3
- Wait Strategy: WebDriverWait with explicit waits

#### Playwright TypeScript Template
- Framework: Playwright
- Language: TypeScript
- Test Runner: Jest with @playwright/test
- Wait Strategy: Built-in Playwright waits

#### Playwright .NET Template
- Framework: Playwright
- Language: C#
- Test Runner: NUnit 3
- Wait Strategy: Playwright built-in waits

## Workflow: Element Selection to Code Generation

### Step 1: Select Elements
1. User clicks "Select Elements" radio button
2. Browser panel shows live webpage
3. User clicks on elements to select them
4. Selected elements appear in the Inspector panel with:
   - Element type (Button, TextBox, Link, etc.)
   - Selector (CSS or XPath)
   - Text content
   - ID and class names

### Step 2: Define Test Steps
1. User clicks "AI" radio button
2. AI section appears with "Add Action" UI
3. User enters action descriptions:
   - "Click the login button"
   - "Enter username: testuser@example.com"
   - "Enter password: SecurePassword123"
   - "Click the login button"
   - "Verify dashboard is displayed"
4. Each action is added to the AI Actions list
5. Actions can be removed individually with the ✕ button

### Step 3: Select Framework
1. User selects target framework from dropdown:
   - Selenium Java (JUnit 5)
   - Selenium C# (NUnit 3)
   - Playwright TypeScript (Jest)
   - Playwright .NET (NUnit 3)

### Step 4: Generate Script
1. User clicks "Generate Script" button
2. System validates:
   - Elements are selected
   - AI mode is active
   - At least one action is defined
   - Framework is selected
   - API key is available (if not, prompts for it)
3. System:
   - Formats elements from scenario: `Element 1: Button, selector: '#loginBtn', text: 'Login', ...`
   - Collects step descriptions from AI Actions panel
   - Loads framework-specific user.txt template
   - Replaces {{ELEMENTS}} with formatted elements
   - Replaces {{STEPS}} with formatted steps
   - Calls AI coordinator with system + user prompts
4. AI generates two code blocks:
   - **Block 1**: Page Object class with locators and action methods
   - **Block 2**: Test class using the Page Object
5. System:
   - Parses response for code blocks
   - Extracts Page Object code (first ```java, ```csharp, or ```typescript block)
   - Extracts Test class code (second code block)
   - Displays in separate tabs

### Step 5: Review and Use Generated Code
1. Page Object tab shows:
   ```java
   public class ApplicationPage {
       private WebDriver driver;
       private By loginButton = By.id("loginBtn");
       
       public ApplicationPage(WebDriver driver) {
           this.driver = driver;
       }
       
       public void clickLoginButton() {
           WebDriverWait wait = new WebDriverWait(driver, Duration.ofSeconds(10));
           wait.until(ExpectedConditions.elementToBeClickable(loginButton)).click();
       }
       
       // ... more methods
   }
   ```

2. Test Class tab shows:
   ```java
   public class ApplicationTest {
       private WebDriver driver;
       private ApplicationPage page;
       
       @BeforeEach
       public void setUp() {
           driver = new ChromeDriver();
           page = new ApplicationPage(driver);
       }
       
       @Test
       public void testLoginFlow() {
           page.clickLoginButton();
           page.enterUsername("testuser@example.com");
           // ... more assertions
       }
       
       @AfterEach
       public void tearDown() {
           driver.quit();
       }
   }
   ```

## Code Components Updated

### MainWindow.xaml.cs

#### GenerateButton_Click()
- Validates AI mode and action definitions
- Calls `PopulateScenarioStepsFromActions()` to update scenario with user actions
- Creates `ScriptGenerationContext` with framework and elements
- Calls `_aiCoordinator.GenerateAsync()` with populated context
- Displays Page Object and Test Class in separate tabs

#### PopulateScenarioStepsFromActions()
- Extracts action descriptions from AIActionsListPanel
- Creates TestStep objects for each action
- Populates scenario.Steps with both elements and actions

#### FormatElementsForPrompt()
- Formats selected elements into readable format for AI
- Output: `Element 1: Button, selector: '#id', text: 'Login', ...`

#### FormatStepsForPrompt()
- Collects action descriptions from UI panel
- Output: `Step 1: Click login button\nStep 2: Enter credentials\n...`

### Application/IPromptBuilder.cs

#### BuildGenerationPrompt()
- **NEW**: Loads framework-specific `user.txt` template as embedded resource
- **NEW**: Formats elements and steps using helper methods
- **NEW**: Injects formatted content into {{ELEMENTS}} and {{STEPS}} placeholders
- Fallback: Uses generic prompt if template not found

#### LoadPromptTemplate()
- **NEW**: Maps framework names to resource folder names
  - "Selenium Java" → SeleniumJava/user.txt
  - "Selenium C#" → SeleniumCSharp/user.txt
  - "Playwright TypeScript" → PlaywrightTypeScript/user.txt
  - "Playwright .NET" → PlaywrightDotNet/user.txt
- **NEW**: Loads from embedded resources using reflection
- **NEW**: Includes error handling with fallback

#### BuildElementsDescription()
- **ENHANCED**: Improved formatting for AI consumption
- Format: `Element 1:\n  - Type: Button\n  - Locator: #loginBtn\n  - Variable: button_1`

#### BuildStepsDescription()
- **ENHANCED**: Formats test steps with sequential numbering
- Format: `Step 1: Click login button\nStep 2: Enter username\n...`

### TestScriptGeneratorTool.csproj

#### EmbeddedResource Items
- **NEW**: Added 9 prompt files as embedded resources
- Ensures prompts are compiled into executable
- Framework-specific templates automatically available at runtime

```xml
<ItemGroup>
  <EmbeddedResource Include="Prompts\system.txt" />
  <EmbeddedResource Include="Prompts\SeleniumJava\system.txt" />
  <EmbeddedResource Include="Prompts\SeleniumJava\user.txt" />
  <!-- ... other frameworks -->
</ItemGroup>
```

## Key Features

### 1. Framework-Aware Code Generation
- AI adapts code to selected framework
- Uses framework-specific syntax and patterns
- Includes proper imports and dependencies
- Follows test runner conventions (JUnit, NUnit, Jest)

### 2. Dynamic Prompt Injection
- Elements and steps are injected into templates at runtime
- No hardcoded prompts
- Customizable for different test scenarios
- Easy to update templates without code changes

### 3. Separated Code Output
- Page Object code in first tab (easy to copy)
- Test class code in second tab (separate from Page Object)
- Both tabs show complete, runnable code
- Ready for copy-paste into actual projects

### 4. Production-Ready Code
- Page Object Model pattern
- Explicit waits for reliability
- Proper error handling
- Framework best practices
- Compilable without modifications

### 5. Flexible Element Selection
- Select from live browser rendering
- Multiple element types supported
- Element properties captured automatically
- Easy to review selected elements before generation

### 6. User-Friendly Actions Panel
- Add multiple test steps sequentially
- Remove individual steps
- Numbered for clarity
- Descriptive text for each action

## Example: End-to-End Workflow

### Scenario: Login Test for Banking Application

#### Input
**Selected Elements:**
- Username input field: `#username`
- Password input field: `#password`
- Login button: `.btn-login`
- Dashboard message: `.welcome-msg`

**Test Steps:**
1. Click username field
2. Type "customer123"
3. Click password field
4. Type "SecurePass2024"
5. Click login button
6. Verify welcome message appears

**Framework:** Selenium Java (JUnit 5)

#### Generated Page Object (Selenium Java)
```java
package com.example.automation;

import org.openqa.selenium.By;
import org.openqa.selenium.WebDriver;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.support.ui.WebDriverWait;
import org.openqa.selenium.support.ui.ExpectedConditions;
import java.time.Duration;

public class ApplicationPage {
    private WebDriver driver;
    private By usernameField = By.id("username");
    private By passwordField = By.id("password");
    private By loginButton = By.className("btn-login");
    private By welcomeMessage = By.className("welcome-msg");
    
    private static final Duration TIMEOUT = Duration.ofSeconds(10);
    
    public ApplicationPage(WebDriver driver) {
        this.driver = driver;
    }
    
    public void clickUsernameField() {
        WebDriverWait wait = new WebDriverWait(driver, TIMEOUT);
        wait.until(ExpectedConditions.elementToBeClickable(usernameField)).click();
    }
    
    public void enterUsername(String username) {
        WebDriverWait wait = new WebDriverWait(driver, TIMEOUT);
        WebElement field = wait.until(ExpectedConditions.presenceOfElementLocated(usernameField));
        field.clear();
        field.sendKeys(username);
    }
    
    public void clickPasswordField() {
        WebDriverWait wait = new WebDriverWait(driver, TIMEOUT);
        wait.until(ExpectedConditions.elementToBeClickable(passwordField)).click();
    }
    
    public void enterPassword(String password) {
        WebDriverWait wait = new WebDriverWait(driver, TIMEOUT);
        WebElement field = wait.until(ExpectedConditions.presenceOfElementLocated(passwordField));
        field.clear();
        field.sendKeys(password);
    }
    
    public void clickLoginButton() {
        WebDriverWait wait = new WebDriverWait(driver, TIMEOUT);
        wait.until(ExpectedConditions.elementToBeClickable(loginButton)).click();
    }
    
    public boolean isWelcomeMessageDisplayed() {
        WebDriverWait wait = new WebDriverWait(driver, TIMEOUT);
        return wait.until(ExpectedConditions.visibilityOfElementLocated(welcomeMessage)).isDisplayed();
    }
}
```

#### Generated Test Class (Selenium Java)
```java
package com.example.automation;

import org.openqa.selenium.WebDriver;
import org.openqa.selenium.chrome.ChromeDriver;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

public class ApplicationTest {
    private WebDriver driver;
    private ApplicationPage page;
    
    @BeforeEach
    public void setUp() {
        driver = new ChromeDriver();
        driver.manage().timeouts().implicitlyWait(java.time.Duration.ofSeconds(10));
        page = new ApplicationPage(driver);
    }
    
    @Test
    public void testLoginFlow() {
        driver.get("https://banking-app.example.com");
        
        page.clickUsernameField();
        page.enterUsername("customer123");
        
        page.clickPasswordField();
        page.enterPassword("SecurePass2024");
        
        page.clickLoginButton();
        
        assertTrue(page.isWelcomeMessageDisplayed(), "Welcome message should be displayed");
    }
    
    @AfterEach
    public void tearDown() {
        if (driver != null) {
            driver.quit();
        }
    }
}
```

## Configuration

### API Keys and LLM Providers

1. **OpenAI**
   - Settings → Select "OpenAI" provider
   - Model: gpt-4, gpt-4-turbo, or gpt-3.5-turbo
   - Enter API key from https://platform.openai.com/api-keys
   - Test connection before use

2. **Groq** (Free, Fast)
   - Settings → Select "Groq" provider
   - Model: mixtral-8x7b-32768 (recommended)
   - Free API key from https://console.groq.com
   - Test connection before use

3. **Ollama** (Local)
   - Settings → Select "Ollama" provider
   - Ensure Ollama is running locally (default: http://localhost:11434)
   - Model: llama2, mistral, or other local models
   - Test connection before use

## Testing the Feature

### Quick Test Scenario

1. **Open Application**
   - Launch TestScriptGeneratorTool.exe
   - Navigate to Sample Website (if provided)

2. **Select Elements**
   - Click "Select Elements" radio button
   - Click on page elements (buttons, inputs, links)
   - Verify elements appear in Inspector panel

3. **Define Steps**
   - Click "AI" radio button
   - Enter: "Click the login button"
   - Enter: "Type admin in username field"
   - Enter: "Type password123 in password field"
   - Enter: "Click submit"
   - Verify all steps listed

4. **Generate Code**
   - Select Framework: "Selenium Java"
   - Click "Generate Script"
   - Review generated Page Object code
   - Review generated Test Class code
   - Verify code is compilable and follows POM pattern

## Troubleshooting

### Issue: "API key required for AI mode" error

**Solution:**
1. Open Settings window (⚙️ button)
2. Select AI provider (OpenAI, Groq, or Ollama)
3. Enter API key for selected provider
4. Click "Test Connection"
5. Verify "Connection successful" message

### Issue: Generated code has syntax errors

**Solution:**
1. Verify selected framework matches target language
2. Check that elements are properly selected
3. Review test steps for clarity
4. Try different LLM provider (AI model might vary)
5. Manually fix obvious issues or regenerate

### Issue: "No elements selected" validation error

**Solution:**
1. Click "Select Elements" radio button
2. Click on actual page elements
3. Verify elements appear in Inspector panel
4. Elements must have valid selectors (ID, class, CSS path)

### Issue: Prompt template not found error

**Solution:**
1. Rebuild project: `dotnet build`
2. Verify Prompts folder structure
3. Check .csproj file has EmbeddedResource entries
4. Application automatically falls back to generic prompt

## Future Enhancements

1. **Custom Prompt Templates**
   - Allow users to upload custom prompt templates
   - Support template variables for organization-specific formats

2. **Assertion Generation**
   - AI automatically generates assertions
   - Capture expected values from user input

3. **Page Object Inheritance**
   - Share common elements across multiple pages
   - Generate base page classes

4. **Test Case Management**
   - Save generated tests to version control
   - Track test execution history

5. **Cross-Browser Testing**
   - Generate tests for multiple browsers
   - Automatic browser capability configuration

6. **Data-Driven Tests**
   - Generate parameterized tests
   - CSV/Excel data source integration

## References

- **Selenium 4 Documentation**: https://www.selenium.dev/documentation/webdriver/
- **JUnit 5 Documentation**: https://junit.org/junit5/docs/current/user-guide/
- **Playwright Documentation**: https://playwright.dev/
- **NUnit Documentation**: https://docs.nunit.org/
- **Page Object Model Pattern**: https://www.selenium.dev/documentation/test_practices/encouraged/page_object_models/
