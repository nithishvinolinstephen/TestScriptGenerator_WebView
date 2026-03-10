# Technical Architecture — Test Automation Script Generator
## AI-First Automation Authoring Platform (Desktop .NET — Final Implementation Spec)

---

# 1. System Vision

A standalone Windows desktop application that enables testers to generate executable automation scripts by selecting UI elements from any web application and invoking an AI model to produce structured, runnable automation code.

Supported output frameworks:
- Selenium Java (JUnit)
- Selenium C# (NUnit)
- Playwright TypeScript (Jest)
- Playwright .NET (NUnit)

Primary generation engine is AI-driven.
Deterministic template generation exists only as fallback.

---

# 2. Generation Strategy

## 2.1 AI-First Generation (Primary)
DOM → ElementDescriptor → ScenarioDefinition → Prompt Builder → LLM Client → Response Parser → Code Validator → Output

## 2.2 Deterministic Generation (Fallback Only)
Triggered when:
- AI generation fails after max retries
- Offline mode is required
- Enterprise policy restricts external calls

---

# 3. Technology Stack

| Area | Technology |
|---|---|
| Runtime | .NET 8 |
| UI Framework | WPF |
| Embedded Browser | Microsoft.Web.WebView2 (Chromium) |
| Browser Messaging | WebView2 WebMessage API |
| Dependency Injection | Microsoft.Extensions.DependencyInjection |
| Logging | Microsoft.Extensions.Logging |
| Fallback Templates | Scriban |
| JSON Serialization | System.Text.Json |
| LLM Communication | HttpClient (async REST) |
| Credential Storage | Windows Credential Manager |
| Plugin Loading | AssemblyLoadContext |

---

# 4. Required NuGet Packages

```
Microsoft.Web.WebView2
Microsoft.Extensions.DependencyInjection
Microsoft.Extensions.Hosting
Microsoft.Extensions.Logging
Microsoft.Extensions.Logging.Console
Scriban
System.Text.Json
```

Windows Credential Manager access via:
```
AdysTech.CredentialManager
```

---

# 5. Secure API Key Management

Cloud AI providers require credentials.

### Storage Rule
API keys MUST be stored in Windows Credential Manager only.

Never stored in:
- appsettings.json
- config files
- registry
- local JSON
- environment variables

### Credential Service Interface

```csharp
public interface ICredentialService
{
    void SaveApiKey(string provider, string apiKey);
    string? GetApiKey(string provider);
    void DeleteApiKey(string provider);
}
```

### Implementation
Uses `AdysTech.CredentialManager` to read/write Windows Credential Manager entries.
Target name convention: `TestAutomationTool_{Provider}` (e.g. `TestAutomationTool_OpenAI`)

---

# 6. Browser Control — WebView2 Messaging Contract

WPF hosts WebView2 control.

### Host → Browser (inject script)
```csharp
await webView.CoreWebView2.ExecuteScriptAsync(jsCode);
```

### Browser → Host (send selection data)
```javascript
window.chrome.webview.postMessage(JSON.stringify(payload));
```

### Host receives message
```csharp
webView.CoreWebView2.WebMessageReceived += (sender, args) =>
{
    string json = args.TryGetWebMessageAsString();
    // Deserialize to ElementDescriptor
};
```

### Message Payload Contract
```json
{
  "tagName": "button",
  "id": "loginBtn",
  "name": "",
  "classList": ["btn", "btn-primary"],
  "attributes": { "type": "submit", "data-qa": "login" },
  "innerText": "Login",
  "cssSelector": "button#loginBtn",
  "xpath": "//button[@id='loginBtn']",
  "boundingRect": { "top": 120, "left": 40, "width": 100, "height": 36 },
  "framePath": [],
  "shadowHostChain": []
}
```

---

# 7. DOM Selection Engine

### JavaScript Overlay Script Behavior

When selection mode is activated:

1. Inject overlay script via `ExecuteScriptAsync`
2. Attach `mouseover` listener — highlight hovered element with blue outline
3. Attach `click` listener — intercept click, call `event.preventDefault()`, `event.stopPropagation()`
4. Extract full element metadata
5. Detect iframe context — walk `window.frameElement` chain
6. Detect shadow DOM — walk `getRootNode()` chain
7. Generate CSS selector using attribute priority (id → data-qa → name → class+tag)
8. Generate XPath using absolute path from document root
9. Post payload to host via `postMessage`
10. Remove overlay on deactivation

### Frame and Shadow DOM Notes
- If element is inside an iframe, `framePath` contains the ordered index of each nested frame
- If element is inside shadow DOM, `shadowHostChain` contains CSS selectors of each shadow host up the chain
- These are captured for locator context but full shadow DOM interaction is handled at script generation time

---

# 8. Domain Models

### 8.1 ElementDescriptor
```csharp
public class ElementDescriptor
{
    public string TagName { get; set; } = "";
    public string? Id { get; set; }
    public string? Name { get; set; }
    public List<string> ClassList { get; set; } = new();
    public Dictionary<string, string> Attributes { get; set; } = new();
    public string InnerText { get; set; } = "";
    public string CssSelector { get; set; } = "";
    public string XPath { get; set; } = "";
    public BoundingRect BoundingRect { get; set; } = new();
    public List<int> FramePath { get; set; } = new();
    public List<string> ShadowHostChain { get; set; } = new();
}
```

### 8.2 ActionType
```csharp
public enum ActionType
{
    Click,
    TypeText,
    SelectDropdown,
    Hover,
    Navigate,
    AssertVisible,
    AssertTextEquals,
    AssertAttribute,
    WaitForElement,
    UploadFile,
    Screenshot
}
```

### 8.3 AssertionRule
```csharp
public class AssertionRule
{
    public string AssertionType { get; set; } = "";
    public string ExpectedValue { get; set; } = "";
    public string? AttributeName { get; set; }
}
```

### 8.4 TestStep
```csharp
public class TestStep
{
    public Guid StepId { get; set; } = Guid.NewGuid();
    public ActionType ActionType { get; set; }
    public ElementDescriptor TargetElement { get; set; } = new();
    public string? InputValue { get; set; }
    public AssertionRule? Assertion { get; set; }
}
```

### 8.5 ScenarioDefinition
```csharp
public class ScenarioDefinition
{
    public string Name { get; set; } = "";
    public List<TestStep> Steps { get; set; } = new();
    public string TargetFramework { get; set; } = "";
    public string Language { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### 8.6 ScriptGenerationContext
```csharp
public class ScriptGenerationContext
{
    public ScenarioDefinition Scenario { get; set; } = new();
    public List<ElementDescriptor> Elements { get; set; } = new();
    public Dictionary<string, string> LocatorMap { get; set; } = new();
    public string Framework { get; set; } = "";
    public string Language { get; set; } = "";
    public string GenerationMode { get; set; } = "AI";
    public string CodingStandards { get; set; } = "";
}
```

### 8.7 ScriptOutput
```csharp
public class ScriptOutput
{
    public string PageObjectCode { get; set; } = "";
    public string TestClassCode { get; set; } = "";
    public string? DataFileContent { get; set; }
    public GenerationMode Mode { get; set; }
    public bool AISucceeded { get; set; }
}
```

---

# 9. Locator Engine

### Priority Order
1. Unique `id` attribute → `By.Id` / `#id`
2. Unique `name` attribute → `By.Name`
3. Unique `data-qa` or `data-testid` attribute → CSS attribute selector
4. Stable CSS selector (tag + unique class combination)
5. XPath (absolute, from document root)

### Locator Definition
```csharp
public class LocatorDefinition
{
    public string PrimaryLocator { get; set; } = "";
    public string LocatorType { get; set; } = "";
    public List<string> Alternatives { get; set; } = new();
    public bool IsUserModified { get; set; }
}
```

Uniqueness is validated against the live DOM by re-querying the element count for the generated selector via `ExecuteScriptAsync`.

---

# 10. AI Settings Model

```csharp
public class AISettings
{
    public string Provider { get; set; } = "OpenAI";
    public string ModelName { get; set; } = "gpt-4o";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 4096;
    public int TimeoutSeconds { get; set; } = 60;
    public int RetryCount { get; set; } = 2;
}
```

### Supported Providers

| Provider | BaseUrl |
|---|---|
| OpenAI | https://api.openai.com/v1/chat/completions |
| Azure OpenAI | https://{resource}.openai.azure.com/openai/deployments/{model}/chat/completions |
| Ollama (local) | http://localhost:11434/api/chat |
| LM Studio (local) | http://localhost:1234/v1/chat/completions |
| Enterprise hosted | User-configured |

---

# 11. Async LLM Client

### Interface
```csharp
public interface ILLMClient
{
    Task<string> GenerateCompletionAsync(
        string prompt,
        AISettings settings,
        CancellationToken cancellationToken = default);
}
```

### Implementation Requirements
- Must be fully async — no `.Result` or `.Wait()` calls
- Use `HttpClient` with `IHttpClientFactory`
- Pass `CancellationToken` through to HTTP call
- Deserialize provider-specific response format
- Throw typed exception on non-success HTTP status

### OpenAI Request Body
```json
{
  "model": "gpt-4o",
  "temperature": 0.2,
  "max_tokens": 4096,
  "messages": [
    { "role": "system", "content": "{systemPrompt}" },
    { "role": "user", "content": "{userPrompt}" }
  ]
}
```

---

# 12. Prompt Template System

### Storage Location
Prompt templates are stored as embedded resources compiled into the assembly.

```
/Prompts/
    SeleniumJava/
        system.txt
        user.txt
    SeleniumCSharp/
        system.txt
        user.txt
    PlaywrightTS/
        system.txt
        user.txt
    PlaywrightDotNet/
        system.txt
        user.txt
```

### System Prompt (Framework-Independent)
```
You are a senior test automation engineer.
Your task is to generate production-ready automation test code.

Rules:
- Return only code. No explanation. No commentary.
- Wrap each file in a fenced code block using the correct language identifier.
- Always generate exactly two files: a Page Object class and a Test class.
- Use Page Object Model pattern strictly.
- Include explicit waits. Never use Thread.Sleep.
- Use the most stable locator available from the provided metadata.
- Do not use placeholder values. Use actual values from the element metadata.
```

### User Prompt Template — Selenium Java
```
Generate a Selenium Java Page Object class and a JUnit 5 test class.

Framework: Selenium 4
Language: Java
Test runner: JUnit 5
Wait strategy: WebDriverWait with ExpectedConditions

Element details:
  Tag:       {{tag}}
  ID:        {{id}}
  Name:      {{name}}
  CSS:       {{css}}
  XPath:     {{xpath}}
  Text:      {{innerText}}

Test steps:
{{steps}}

Output requirements:
- Page Object class wrapped in ```java``` code fence
- Test class wrapped in ```java``` code fence
- Page Object must have one method per action
- Test class must call Page Object methods only
- Include all required imports
```

### User Prompt Template — Selenium C#
```
Generate a Selenium C# Page Object class and an NUnit test class.

Framework: Selenium 4
Language: C#
Test runner: NUnit 3
Wait strategy: WebDriverWait with ExpectedConditions

Element details:
  Tag:       {{tag}}
  ID:        {{id}}
  Name:      {{name}}
  CSS:       {{css}}
  XPath:     {{xpath}}
  Text:      {{innerText}}

Test steps:
{{steps}}

Output requirements:
- Page Object class wrapped in ```csharp``` code fence
- Test class wrapped in ```csharp``` code fence
- Use PageFactory or direct By locators
- Include all required using statements
```

### User Prompt Template — Playwright TypeScript
```
Generate a Playwright TypeScript Page Object class and a Jest test file.

Framework: Playwright
Language: TypeScript
Test runner: Jest with @playwright/test
Wait strategy: Playwright built-in auto-waiting

Element details:
  Tag:       {{tag}}
  ID:        {{id}}
  CSS:       {{css}}
  XPath:     {{xpath}}
  Text:      {{innerText}}

Test steps:
{{steps}}

Output requirements:
- Page Object class wrapped in ```typescript``` code fence
- Test file wrapped in ```typescript``` code fence
- Use locator API not $ or evaluate
- Include all required imports
```

### User Prompt Template — Playwright .NET
```
Generate a Playwright .NET Page Object class and an NUnit test class.

Framework: Playwright for .NET
Language: C#
Test runner: NUnit 3

Element details:
  Tag:       {{tag}}
  ID:        {{id}}
  CSS:       {{css}}
  XPath:     {{xpath}}
  Text:      {{innerText}}

Test steps:
{{steps}}

Output requirements:
- Page Object class wrapped in ```csharp``` code fence
- Test class wrapped in ```csharp``` code fence
- Use IPage locator API
- Include all required using statements
```

---

# 13. Prompt Builder

```csharp
public interface IPromptBuilder
{
    (string systemPrompt, string userPrompt) Build(ScriptGenerationContext context);
}
```

### Build Logic
1. Load system.txt and user.txt from embedded resources for the selected framework
2. Replace `{{tag}}`, `{{id}}`, `{{name}}`, `{{css}}`, `{{xpath}}`, `{{innerText}}` from `ElementDescriptor`
3. Build `{{steps}}` by iterating `ScenarioDefinition.Steps` and formatting each as natural language:
   - `Click` → "Click the element"
   - `TypeText` → "Type '{InputValue}' into the element"
   - `AssertVisible` → "Assert the element is visible"
   - `AssertTextEquals` → "Assert element text equals '{ExpectedValue}'"
4. Return both prompts as a tuple

---

# 14. Response Parser

### Responsibility
Extract Page Object code and Test class code from raw LLM response string.

### Parser Rules (in order)

1. Scan response for all fenced code blocks using pattern ` ```{language} ... ``` `
2. If two or more blocks found:
   - Identify Page Object block: block whose content contains a class name matching `*Page`, `*PageObject`, or `*Component`
   - Identify Test class block: block whose content contains `@Test`, `[Test]`, `test(`, or `it(`
   - If pattern match fails, assign first block to Page Object, second to Test class
3. If one block found: treat as Test class, generate empty Page Object warning
4. If no blocks found: treat entire response as combined code, attempt to split on `public class` boundaries
5. Strip all markdown prose outside code blocks
6. Return `ParsedResponse` with `PageObjectCode` and `TestClassCode` strings

### ParsedResponse Model
```csharp
public class ParsedResponse
{
    public string PageObjectCode { get; set; } = "";
    public string TestClassCode { get; set; } = "";
    public bool ParseSucceeded { get; set; }
    public string? ParseWarning { get; set; }
}
```

---

# 15. Code Validation Engine

### Interface
```csharp
public interface ICodeValidator
{
    ValidationResult Validate(ParsedResponse parsed, string framework);
}
```

### Validation Checks (heuristic, no compiler required)

| Check | Rule |
|---|---|
| Class exists | Response contains `class ` keyword |
| No placeholders | Response does not contain `{{` or `}}` |
| Required imports present | Language-specific import keywords exist |
| Methods present | Response contains `void ` or `public ` method signatures |
| Framework API usage | Language-specific API terms present (e.g. `WebDriverWait`, `driver.findElement`) |

### ValidationResult
```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Failures { get; set; } = new();
}
```

---

# 16. Retry Engine

### Strategy

```
Attempt 1: Send original prompt
  → On failure: log reason

Attempt 2: Append repair instruction to prompt
  "Previous attempt failed validation: {failures}. Fix these issues and return only valid code."
  → On failure: log reason

Attempt 3: Fallback to deterministic generator
  → Log AI failure, set ScriptOutput.AISucceeded = false
```

### Max Retries
Controlled by `AISettings.RetryCount` (default: 2 AI attempts before fallback).

---

# 17. AI Generation Coordinator

```csharp
public interface IAIGenerationCoordinator
{
    Task<ScriptOutput> GenerateAsync(
        ScriptGenerationContext context,
        IProgress<string> progress,
        CancellationToken cancellationToken);
}
```

### Execution Flow
1. Call `IPromptBuilder.Build(context)` → get system + user prompts
2. Call `ILLMClient.GenerateCompletionAsync()` with prompts and settings
3. Call `IResponseParser.Parse()` on raw LLM string
4. Call `ICodeValidator.Validate()` on parsed response
5. If valid → return `ScriptOutput`
6. If invalid and retries remain → repair prompt and repeat from step 2
7. If retries exhausted → call deterministic fallback generator
8. Report progress at each step via `IProgress<string>`

---

# 18. Plugin System

### Interface
```csharp
public interface IAutomationGeneratorPlugin
{
    string FrameworkName { get; }
    string Language { get; }
    ScriptOutput Generate(ScriptGenerationContext context);
}
```

### Loading
Plugins loaded from `/Plugins/` directory using `AssemblyLoadContext`.
Each plugin assembly must implement `IAutomationGeneratorPlugin`.
Plugins register themselves via DI on load.

---

# 19. Locator Editing UI Requirements

- After element is selected, show locator panel
- Display primary locator with type label
- Display alternatives list
- Allow user to manually override locator text
- Set `LocatorDefinition.IsUserModified = true` on override
- Validate modified locator against live DOM before accepting

---

# 20. WPF UI Component Map

| Component | Responsibility |
|---|---|
| MainWindow | Shell, menu, region host |
| BrowserPanel | WebView2 host, navigation bar |
| SelectionModeToggle | Activate/deactivate overlay script |
| ElementInspectorPanel | Show captured ElementDescriptor properties |
| LocatorPanel | Show and edit LocatorDefinition |
| StepBuilderPanel | ActionType dropdown, input field, add step button |
| ScenarioPanel | Ordered list of TestStep records |
| FrameworkSelector | Dropdown for Selenium Java / C# / Playwright TS / .NET |
| GenerateModeSelector | AI vs Deterministic radio |
| GenerateButton | Trigger GenerateAsync, show spinner |
| OutputPanel | Display PageObjectCode and TestClassCode in tabs |
| CopyButton | Copy selected tab content to clipboard |
| SettingsWindow | AI provider, model, key, URL, temperature |

---

# 21. Phase Implementation Plan

---

## Phase 0 — Project Foundation
- Create solution with projects: `App`, `Core`, `Infrastructure`, `Plugins`
- Configure DI container with all service interfaces
- Configure `Microsoft.Extensions.Logging`
- Verify: application launches with empty shell

---

## Phase 1 — WebView2 Browser Integration
- Add WebView2 to WPF shell
- Implement navigation bar (URL input + Go button)
- Handle `NavigationCompleted` event
- Verify: any public website loads

---

## Phase 2 — DOM Selection Engine
- Implement overlay injection script
- Implement hover highlight (blue outline via CSS injection)
- Implement click interception with `preventDefault`
- Implement metadata extraction (all ElementDescriptor fields)
- Implement frame path detection
- Implement shadow DOM chain detection
- Implement CSS selector generation
- Implement XPath generation
- Post payload via `postMessage`
- Receive and deserialize in WPF host
- Display raw metadata in inspector panel
- Verify: click any element, see full metadata

---

## Phase 3 — Locator Engine
- Implement locator priority algorithm
- Validate locator uniqueness against DOM
- Display in locator panel with edit capability
- Verify: locator uniquely identifies element

---

## Phase 4 — Scenario Builder
- Implement `TestStep` creation UI
- Implement step list display
- Implement step reordering and deletion
- Implement `ScenarioDefinition` in-memory storage
- Verify: multi-step scenario built and displayed

---

## Phase 5 — Minimal Deterministic Generator
- Implement `Scriban` template loading from embedded resources
- Implement one Selenium Java template (Page Object + Test class)
- Implement `ScriptOutput` and display in output panel
- Verify: basic generated code appears in output panel

This phase proves the generation pipeline. It is not feature-complete.

---

## Phase 6 — AI Generation Engine (CORE PHASE)

### Step 6.1 — LLM Client
- Implement `ILLMClient` with `HttpClient`
- Implement OpenAI adapter
- Implement Ollama adapter (local model)
- Implement `ICredentialService` with Windows Credential Manager
- Add `AISettings` with `BaseUrl`

### Step 6.2 — Prompt Builder
- Load prompt templates from embedded resources
- Implement variable substitution for all ElementDescriptor fields
- Implement step formatting for all ActionType values

### Step 6.3 — Response Parser
- Implement fenced code block extraction by regex
- Implement class-name-based block classification (Page Object vs Test)
- Implement fallback for unfenced responses

### Step 6.4 — Code Validator
- Implement all heuristic checks
- Return `ValidationResult` with failure list

### Step 6.5 — Retry Controller
- Implement attempt loop with repair prompt
- Implement deterministic fallback trigger

### Step 6.6 — UI Integration
- Wire Generate button to `IAIGenerationCoordinator.GenerateAsync`
- Show progress messages during generation
- Implement CancellationToken wired to Cancel button
- Display output in tabbed panel (Page Object tab / Test Class tab)
- Implement Copy to Clipboard button per tab

Verify: Select element → choose Selenium Java → click Generate → AI returns runnable test class.

---

## Phase 7 — Multi-Framework Support
- Add prompt templates for Selenium C#, Playwright TypeScript, Playwright .NET
- Add framework selection UI
- Wire framework selection to `ScriptGenerationContext`
- Verify: same scenario generates correct code for all four frameworks

---

## Phase 8 — Hybrid Mode Selector
- Add AI vs Deterministic radio button
- Complete deterministic templates for all four frameworks
- Wire mode selection to coordinator
- Verify: deterministic mode produces runnable output without AI call

---

## Phase 9 — Plugin Infrastructure
- Implement `AssemblyLoadContext` plugin loader
- Scan `/Plugins/` directory on startup
- Register discovered plugins in DI
- Expose plugin frameworks in framework selector
- Verify: external plugin produces output via standard UI

---

# 22. Error Handling Reference

| Scenario | Handling |
|---|---|
| WebView2 not installed | Show install prompt with download link |
| Script injection fails | Retry once, log error, notify user |
| Invalid message payload | Log and ignore, do not crash |
| AI HTTP timeout | Surface timeout message, offer retry |
| AI returns empty response | Treat as validation failure, trigger retry |
| AI key missing | Prompt user to open Settings and enter key |
| All AI retries exhausted | Fall back to deterministic, notify user |
| Plugin load failure | Log error, skip plugin, continue startup |

---

# 23. Security Model

- No browser extensions installed or required
- Local execution only — no telemetry
- No credential stored in plain text
- API key access via Windows Credential Manager only
- Prompt content limited to element metadata — no page content scraped
- Offline model supported (Ollama, LM Studio)
- AI mode is optional — tool is fully functional in deterministic mode without network

---

# 24. Success Criteria

1. User opens any web application in embedded browser
2. Activates selection mode
3. Clicks a UI element
4. Element metadata is captured and displayed
5. User defines action (e.g. Click)
6. User selects framework (e.g. Selenium Java)
7. User clicks Generate
8. AI returns Page Object class and Test class
9. User copies code
10. Code runs against the original application without modification

---

# 25. Future Enhancements (Post-MVP)

- Auto assertion inference from element type
- Locator self-healing using AI
- Full test suite generation from sitemap
- Authenticated session capture
- Test execution runner with result display
- AI-suggested waits based on element type
- Cloud orchestration for enterprise teams

---

END OF FINAL IMPLEMENTATION ARCHITECTURE