# Technical Architecture — Test Automation Script Generator

## Desktop .NET Implementation Specification (MVP)

---

# 1. System Overview

## 1.1 Goal

Standalone Windows desktop application that enables manual testers to select web UI elements and generate executable automation scripts across multiple frameworks.

The system performs:

* DOM inspection via embedded Chromium browser
* Deterministic locator generation
* Structured step modeling
* Template-driven code generation
* Plugin-based extensibility

---

# 2. Technology Stack (Explicit)

## 2.1 Platform

| Area                 | Technology                               |
| -------------------- | ---------------------------------------- |
| Runtime              | .NET 8                                   |
| UI Framework         | WPF                                      |
| Embedded Browser     | Microsoft WebView2 (Chromium)            |
| Browser Messaging    | WebView2 WebMessage API                  |
| Dependency Injection | Microsoft.Extensions.DependencyInjection |
| Logging              | Microsoft.Extensions.Logging             |
| Template Engine      | Scriban                                  |
| Serialization        | System.Text.Json                         |
| Plugin Loading       | .NET AssemblyLoadContext                 |

---

## 2.2 Required NuGet Packages

```text
Microsoft.Web.WebView2
Microsoft.Extensions.DependencyInjection
Microsoft.Extensions.Logging
Microsoft.Extensions.Logging.Console
Scriban
System.Text.Json
```

---

# 3. Layered Architecture

```
Presentation Layer (WPF)
Application Layer (Session + Workflow)
Domain Layer (Models)
Infrastructure Layer (Browser + Generation + Storage)
```

---

# 4. Browser Control Architecture (WebView2)

## 4.1 Browser Hosting

WPF hosts WebView2 control.

Navigation handled by BrowserController.

---

## 4.2 WPF ↔ Browser Messaging Contract

### Direction: JS → Host

```js
window.chrome.webview.postMessage(JSON.stringify(payload))
```

Handled in WPF:

```
CoreWebView2.WebMessageReceived
```

---

### Direction: Host → JS

```
ExecuteScriptAsync()
```

Used for:

* Enable selection mode
* Disable selection mode
* Inject highlight overlay

---

## 4.3 Message Types

```
ELEMENT_SELECTED
SELECTION_MODE_ENABLED
SELECTION_MODE_DISABLED
SCRIPT_ERROR
```

---

# 5. DOM Selection Engine (Critical Phase)

## 5.1 Injection Strategy

On selection mode activation:

1. Inject overlay script
2. Register mousemove listener
3. Highlight hovered element
4. Intercept click
5. Prevent default click
6. Extract metadata
7. Send payload to host

---

## 5.2 Metadata Extraction Algorithm

Capture:

* tagName
* id
* name
* classList
* attributes
* text
* CSS selector
* XPath
* bounding rectangle
* iframe path
* shadow host path

---

## 5.3 Shadow DOM Strategy

MVP behavior:

* Detect shadow root
* Capture host chain
* Store full path

Automation generation uses JS execution fallback.

---

## 5.4 Iframe Strategy

Capture hierarchical frame index chain.

Example:

```
main → frame[1] → frame[0]
```

---

# 6. Domain Models (Implementation Ready)

## 6.1 ElementDescriptor

```csharp
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
```

---

## 6.2 ActionType Enum

```csharp
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
```

---

## 6.3 AssertionRule

```csharp
class AssertionRule
{
    string AssertionType;
    string ExpectedValue;
    string? AttributeName;
}
```

---

## 6.4 TestStep

```csharp
class TestStep
{
    Guid StepId;
    ActionType ActionType;
    ElementDescriptor TargetElement;
    string? InputValue;
    AssertionRule? Assertion;
}
```

---

## 6.5 ScenarioDefinition

```csharp
class ScenarioDefinition
{
    string Name;
    List<TestStep> Steps;
    string TargetFramework;
}
```

---

## 6.6 ScriptGenerationContext

```csharp
class ScriptGenerationContext
{
    ScenarioDefinition Scenario;
    List<ElementDescriptor> Elements;
    Dictionary<string,string> LocatorMap;
    string Framework;
    string Language;
}
```

---

# 7. Locator Engine

Priority order:

1. Unique ID
2. Unique name
3. Stable CSS path
4. XPath

Validation rule:

Locator must return exactly one element in DOM.

---

# 8. Script Generation Architecture

Pipeline:

```
Scenario → Locator Map → Page Object Builder → Test Builder → Data Builder → Template Render
```

Template rendering via Scriban.

---

# 9. Plugin System

Plugins implement:

```csharp
interface IAutomationGeneratorPlugin
{
    string FrameworkName;
    ScriptOutput Generate(ScriptGenerationContext context);
}
```

---

# 10. Error Handling Strategy

## Browser

* Script injection failure → retry once
* Selection failure → notify UI

## Locator

* Non-unique locator → fallback hierarchy

## Generation

* Template failure → abort generation

## Messaging

* Invalid payload → ignore and log

---

# 11. UI Workflow

1. Navigate page
2. Enable selection
3. Receive element metadata
4. Configure step
5. Build scenario
6. Generate code

---

# 12. Phase Implementation Plan (Expanded)

## Phase 0 — Foundation

Solution structure, DI, logging.

---

## Phase 1 — WebView2 Integration

Host browser and navigation.

---

## Phase 2 — DOM Selection (High Risk Phase)

Implementation Steps:

1. Load JS overlay module
2. Register hover listener
3. Draw highlight box
4. Intercept click
5. Prevent navigation
6. Serialize element
7. Post message to host
8. Deserialize in WPF
9. Map to ElementDescriptor

Validation:

Must work on:

* nested elements
* dynamic DOM
* iframe
* shadow DOM detection

---

## Phase 3 — Locator Generation

Unique locator algorithm.

---

## Phase 4 — Step Modeling

Scenario builder.

---

## Phase 5 — Script Generation

Validation requirement:

Generated script must:

* compile
* launch browser
* locate element
* perform action
* exit without error

External dependency:

User must have automation runtime installed (Playwright / Selenium).

---

# 13. External Runtime Dependencies

For script validation:

| Framework  | Required           |
| ---------- | ------------------ |
| Playwright | playwright install |
| Selenium   | browser driver     |

---

# 14. Security Constraints

* No browser extensions
* Local execution only
* No telemetry
* No credential storage

---

# 15. Engineering Advancement Rules

Do not move phase until:

* feature stable
* validated
* logged
* documented

---

# 16. Success Criteria

User selects element → builds steps → generates script → script executes.

---

END OF ARCHITECTURE
