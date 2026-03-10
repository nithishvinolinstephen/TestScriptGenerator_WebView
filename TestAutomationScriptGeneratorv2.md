# Technical Architecture — Test Automation Script Generator
## Hybrid Deterministic + AI-Driven Automation Authoring Platform (Desktop .NET)

---

# 1. System Vision

A standalone Windows desktop application that enables testers to generate executable automation scripts by selecting UI elements from web applications and generating automation code using either:

1. Deterministic template-based generation
2. AI-driven LLM-based generation

The system supports multi-framework automation and enterprise-safe execution without browser extensions.

---

# 2. Generation Modes

## 2.1 Deterministic Mode
Rule-based template rendering.

Flow:
DOM → Models → Templates → Code

Characteristics:
- Fast
- Predictable
- Offline capable
- No cost
- Enterprise safe

---

## 2.2 AI Generation Mode
LLM generates test code from structured prompt.

Flow:
DOM → Scenario Model → Prompt Builder → LLM → Code → Validation

Characteristics:
- Flexible
- Context-aware
- Multi-style output
- Requires model access

---

# 3. Technology Stack

| Area | Technology |
|---|---|
| Runtime | .NET 8 |
| UI | WPF |
| Embedded Browser | Microsoft WebView2 (Chromium) |
| Browser Messaging | WebView2 WebMessage API |
| Dependency Injection | Microsoft.Extensions.DependencyInjection |
| Logging | Microsoft.Extensions.Logging |
| Template Engine | Scriban |
| JSON | System.Text.Json |
| LLM Communication | HTTP REST |
| Plugin Loading | AssemblyLoadContext |

---

# 4. Required NuGet Packages

- Microsoft.Web.WebView2
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Logging.Console
- Scriban
- System.Text.Json

---

# 5. High-Level Architecture

WPF UI  
→ Application Orchestrator  
→ Scenario Builder  
→ Generation Mode Selector  
→ (Template Generator OR AI Generation Engine)

---

# 6. Browser Control Architecture (WebView2)

WPF hosts WebView2.

Communication uses:

Host → Browser:
ExecuteScriptAsync()

Browser → Host:
window.chrome.webview.postMessage(JSON.stringify(payload))

Handled via CoreWebView2.WebMessageReceived.

---

# 7. DOM Selection Engine

When selection mode enabled:

1. Inject overlay script
2. Highlight hovered element
3. Intercept click
4. Prevent default navigation
5. Extract metadata
6. Send metadata to host

Captured metadata:

- tagName
- id
- name
- classList
- attributes
- text
- CSS selector
- XPath
- bounding rectangle
- iframe hierarchy
- shadow DOM host chain

---

# 8. Domain Models

## 8.1 ElementDescriptor

class ElementDescriptor  
{
    string TagName;
    string? Id;
    string? Name;
    List<string> ClassList;
    Dictionary<string,string> Attributes;
    string InnerText;
    string CssSelector;
    string XPath;
    List<int> FramePath;
    List<string> ShadowHostChain;
}

---

## 8.2 ActionType

enum ActionType  
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

---

## 8.3 AssertionRule

class AssertionRule  
{
    string AssertionType;
    string ExpectedValue;
    string? AttributeName;
}

---

## 8.4 TestStep

class TestStep  
{
    Guid StepId;
    ActionType ActionType;
    ElementDescriptor TargetElement;
    string? InputValue;
    AssertionRule? Assertion;
}

---

## 8.5 ScenarioDefinition

class ScenarioDefinition  
{
    string Name;
    List<TestStep> Steps;
    string TargetFramework;
}

---

## 8.6 ScriptGenerationContext

class ScriptGenerationContext  
{
    ScenarioDefinition Scenario;
    List<ElementDescriptor> Elements;
    Dictionary<string,string> LocatorMap;
    string Framework;
    string Language;
    string GenerationMode;
    string CodingStandards;
}

---

# 9. Locator Engine

Priority:

1. Unique ID
2. Unique name
3. Stable CSS
4. XPath

Locator must uniquely identify element.

---

# 10. Deterministic Script Generation

Pipeline:

Scenario → Locator Map → Page Object Builder → Test Builder → Data Builder → Template Render

Templates rendered via Scriban.

---

# 11. AI Generation Subsystem

Primary responsibilities:

- Construct prompt
- Send to LLM
- Parse response
- Validate generated code
- Retry if invalid
- Fallback to deterministic generator

---

## 11.1 AI Generation Pipeline

ScenarioDefinition  
→ Prompt Builder  
→ LLM Client  
→ Response Parser  
→ Code Validator  
→ Result

---

## 11.2 Prompt Content

Includes:

- framework
- language
- locators
- actions
- coding rules
- structure requirements
- output format

---

## 11.3 LLM Client Interface

interface ILLMClient  
{
    string GenerateCompletion(string prompt, AISettings settings);
}

---

## 11.4 Supported Model Types

- Cloud API (OpenAI, Azure)
- Enterprise hosted inference
- Local models (Ollama, LM Studio)

---

## 11.5 Code Validation

Checks:

- syntax structure
- class existence
- required imports
- API usage consistency

Optional future:
compile test

---

## 11.6 Retry Strategy

If validation fails:

1. Repair prompt
2. Regenerate
3. Fallback to template mode

---

# 12. AI Settings

class AISettings  
{
    string Provider;
    string ModelName;
    double Temperature;
    int MaxTokens;
    int TimeoutSeconds;
    int RetryCount;
}

---

# 13. Plugin System

interface IAutomationGeneratorPlugin  
{
    string FrameworkName;
    ScriptOutput Generate(ScriptGenerationContext context);
}

---

# 14. Error Handling

Browser:
retry injection once

Locator:
fallback hierarchy

AI:
retry → repair → fallback

Messaging:
log and ignore invalid payload

---

# 15. Security Model

- No browser extensions
- Local execution only
- No credential capture
- AI optional
- Prompt size limits
- Offline model supported

---

# 16. Phase Implementation Plan

Phase 0 — Project foundation  
Phase 1 — WebView2 integration  
Phase 2 — DOM selection engine  
Phase 3 — Locator generation  
Phase 4 — Scenario builder  
Phase 5 — Deterministic script generation (first usable product)  
Phase 6 — AI generation engine  
Phase 7 — Hybrid mode selector  
Phase 8 — Plugin infrastructure  

---

# 17. External Dependencies

For script validation:

Playwright → playwright install  
Selenium → browser driver

For AI mode:

Model endpoint or local runtime

---

# 18. Success Criteria

User selects element → chooses generation mode → receives executable automation test class.

---

# 19. Future Enhancements

- AI locator optimization
- assertion recommendation
- self healing
- test refactoring
- automatic waits
- full test suite generation

---

END OF ARCHITECTURE