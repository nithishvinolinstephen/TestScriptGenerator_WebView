# Phase 6: AI Generation Engine

## Overview
Phase 6 implements AI-powered test script generation with support for both OpenAI and local Ollama models. Users can toggle between deterministic (Phase 5) and AI-based generation, configure credentials via a settings dialog, and generate test automation scripts using large language models.

## Architecture

### Core Components

#### 1. **AISettings** (`Core/AISettings.cs`)
Configuration class for AI generation with properties:
- `Enabled` - Whether AI generation is active
- `Provider` - LLM provider (OpenAI, Ollama)
- `Model` - Model name (e.g., gpt-4, llama2)
- `BaseUrl` - API endpoint URL
- `ApiKey` - API authentication key
- `Temperature` - Sampling temperature (0.0-1.0)
- `MaxRetries` - Number of retry attempts on validation failure
- `MaxTokens` - Maximum tokens in response
- `TimeoutSeconds` - HTTP request timeout

#### 2. **Credential Storage** (`Infrastructure/ICredentialService.cs`)
Secure credential management:
- **Interface**: `ICredentialService`
  - `StoreCredentialAsync(key, value)` - Securely store credentials
  - `GetCredentialAsync(key)` - Retrieve credentials
  - `DeleteCredentialAsync(key)` - Remove stored credentials
  - `CredentialExistsAsync(key)` - Check credential existence
  
- **Implementation**: `WindowsCredentialService`
  - File-based storage in AppData/TestScriptGeneratorTool
  - Credentials encrypted at rest
  - Per-provider credential isolation (OpenAI.ApiKey, Ollama credentials, etc.)

#### 3. **LLM Client Abstraction** (`Application/ILLMClient.cs`)
Unified interface for LLM providers:
- **Interface**: `ILLMClient`
  - `GenerateAsync(prompt)` - Generate text from prompt
  - `HealthCheckAsync()` - Verify connectivity
  
- **LLMResponse Model**:
  - `Content` - Generated text
  - `TotalTokens` - Token usage count
  - `IsComplete` - Whether generation completed successfully
  - `FinishReason` - Completion reason (stop, length, error, etc.)

#### 4. **OpenAI Integration** (`Application/OpenAIClient.cs`)
Connects to OpenAI Chat Completions API:
- **Endpoint**: POST `/chat/completions`
- **Auth**: Bearer token in Authorization header
- **Model**: gpt-4 (configurable)
- **Features**:
  - Streaming-compatible payload structure
  - Token counting from usage response
  - Automatic retry on transient failures
  - Request/response logging for debugging
  
**Example Usage**:
```csharp
var client = new OpenAIClient(httpClient, aiSettings, logger);
var response = await client.GenerateAsync("Write a test case for login");
```

#### 5. **Ollama Integration** (`Application/OllamaClient.cs`)
Connects to local Ollama `/api/generate` endpoint:
- **Endpoint**: POST `/api/generate`
- **Auth**: No authentication (local)
- **Models**: llama2, mistral, neural-chat, etc. (pre-downloaded)
- **Features**:
  - Local-first execution (no external API calls)
  - No token counting (local models)
  - Streaming response handling
  - Health check via model list endpoint
  
**Example Usage**:
```csharp
var client = new OllamaClient(httpClient, aiSettings, logger);
var response = await client.GenerateAsync("Write a test case for login");
```

#### 6. **Prompt Engineering** (`Application/IPromptBuilder.cs`)
Dynamic prompt generation:
- **Interface**: `IPromptBuilder`
  - `BuildGenerationPrompt(context)` - Initial generation prompt
  - `BuildRepairPrompt(context, failures)` - Retry prompt with validation feedback
  
- **Implementation**: `PromptBuilder`
  - Constructs system prompts for Java Selenium code generation
  - Includes framework requirements, import statements, pattern examples
  - Generates targeted repair prompts for failed validations
  
**Prompt Structure**:
```
System: You are an expert test automation engineer...
User: Generate Page Object for [scenario]
- Elements: [element list]
- Actions: [step list]
Return: Java code with imports, class, constructor, @FindBy locators
```

#### 7. **Response Parsing** (`Application/IResponseParser.cs`)
Extracts and classifies generated code:
- **Interface**: `IResponseParser`
  - `ExtractCodeBlocks(response)` - Extract ```java...``` blocks
  - `ClassifyCodeBlock(code)` - Identify Page Object vs Test class
  - `GeneratePlaceholderPageObject(scenario)` - Fallback placeholder
  
- **Implementation**: `ResponseParser`
  - Regex-based code block extraction
  - Pattern matching for class declarations
  - Handles both fenced (```java) and unfenced code blocks
  - Generates minimal Page Object if extraction fails
  
**Example Classification**:
```
"public class LoginPage {" + public WebDriver driver;" 
→ ClassType.PageObject
"public class LoginTest {" + "@Test"
→ ClassType.TestClass
```

#### 8. **Code Validation** (`Application/ICodeValidator.cs`)
Validates generated Java code quality:
- **Interface**: `ICodeValidator`
  - `ValidatePageObject(code)` - Check Page Object structure
  - `ValidateTestClass(code)` - Check test class structure
  
- **Validation Rules**:
  - **Page Object**:
    - ✓ Public class declaration
    - ✓ WebDriver field with private access
    - ✓ Constructor accepting WebDriver
    - ✓ PageFactory.initElements() call
    - ✓ Imports: org.openqa.selenium, org.openqa.selenium.support
  
  - **Test Class**:
    - ✓ Public class declaration
    - ✓ Test methods with @Test annotation
    - ✓ @Before and @After lifecycle methods
    - ✓ JUnit imports (org.junit.*)
    - ✓ WebDriver teardown in @After
  
- **Result**: `ValidationResult`
  - `IsValid` - Boolean pass/fail
  - `Failures` - List of specific validation errors

#### 9. **Orchestrator** (`Application/IAIGenerationCoordinator.cs`)
Coordinates full AI generation pipeline:
- **Interface**: `IAIGenerationCoordinator`
  - `GenerateAsync(context, cancellationToken)` - Execute full pipeline
  
- **Pipeline Flow**:
  1. Health check (verify LLM connectivity)
  2. Build initial prompt via PromptBuilder
  3. Call LLMClient.GenerateAsync()
  4. Parse response via ResponseParser
  5. Validate code via CodeValidator
  6. If valid → return success
  7. If invalid AND retries remain:
     - Build repair prompt with failure reasons
     - Retry from step 2
  8. If retries exhausted → fallback to deterministic generation
  
- **Features**:
  - Automatic retry loop with exponential backoff
  - Validation-driven repairs with specific feedback
  - Deterministic fallback on failure
  - CancellationToken support for cancellation
  - Comprehensive error logging
  - Returns ScriptOutput with generated code or error details

### UI Components

#### 1. **Settings Window** (`SettingsWindow.xaml` / `SettingsWindow.xaml.cs`)
Configuration dialog for AI settings:
- **Provider Selection**: OpenAI ↔ Ollama radio buttons
- **Model Configuration**: Model name, Base URL fields
- **API Key**: Secure PasswordBox input
- **Model Parameters**: 
  - Temperature slider (0.0-1.0)
  - Max Retries spinner
  - Max Tokens spinner
- **Actions**:
  - **Save**: Validates inputs, stores credentials, updates AISettings
  - **Test Connection**: Verifies LLM connectivity before saving
  - **Cancel**: Discards changes
- **Status Feedback**: Real-time status messages with color coding

#### 2. **MainWindow Enhancements** (`MainWindow.xaml` / `MainWindow.xaml.cs`)
New toolbar button and generation logic:
- **⚙ Settings Button**: Opens SettingsWindow dialog
- **Mode Toggle**: 
  - Deterministic radio (default)
  - AI radio (enables AI generation)
- **Updated Generation Logic**:
  - Checks mode selection
  - For AI mode:
    - Prompts for API key if not stored
    - Routes to AICoordinator
  - For Deterministic mode:
    - Routes to ScriptGenerator (Phase 5)

## Service Registration

All Phase 6 services are registered in `Core/ServiceProvider.cs`:

```csharp
// Configuration
services.AddSingleton<AISettings>(new AISettings 
{ 
    Enabled = false,  // Disabled by default
    Provider = "OpenAI",
    Model = "gpt-4",
    BaseUrl = "https://api.openai.com/v1",
    Temperature = 0.2,
    MaxRetries = 3,
    MaxTokens = 4000
});

// Credential Storage
services.AddSingleton<ICredentialService>(sp => 
    new WindowsCredentialService());

// HTTP Client for OpenAI
services.AddHttpClient<OpenAIClient>();

// LLM Clients
services.AddSingleton<ILLMClient, OpenAIClient>();

// Code Generation Utilities
services.AddSingleton<IPromptBuilder, PromptBuilder>();
services.AddSingleton<IResponseParser, ResponseParser>();
services.AddSingleton<ICodeValidator, CodeValidator>();

// Orchestrator
services.AddSingleton<IAIGenerationCoordinator, AIGenerationCoordinator>();
```

## Workflow

### User Configures API Key
1. User clicks **⚙ Settings** button
2. SettingsWindow opens with current settings preloaded
3. User enters OpenAI API key (or selects Ollama with base URL)
4. User clicks **Test Connection** to verify credentials
5. User clicks **Save** to store credentials securely and close dialog

### User Generates with AI
1. User selects test elements and builds test scenario (Phases 1-4)
2. User clicks **AI** radio button
3. User clicks **Generate Script** button
4. System:
   - Checks if API key is stored
   - If missing: Prompts user to run Settings first
   - If present: Executes AICoordinator pipeline
   - Shows progress status
5. Generated Page Object and Test Class appear in output panel
6. User can copy or save output

### Generation with Retry Loop
If initial generation produces invalid code:
1. AICoordinator validates generated code
2. If invalid, builds "repair prompt" with specific failure reasons
3. Re-submits to LLM with feedback
4. Validates updated code
5. Repeats until valid or max retries exceeded
6. On exhaustion, falls back to deterministic generation

## Configuration Files

### Credential Storage Location
- **Windows**: `%APPDATA%/TestScriptGeneratorTool/credentials.json`
- **Format**: JSON with provider-scoped keys
  ```json
  {
    "OpenAI.ApiKey": "[encrypted-key]",
    "Ollama.BaseUrl": "http://localhost:11434"
  }
  ```

### AISettings Defaults
```csharp
public class AISettings
{
    public bool Enabled { get; set; } = false;
    public string Provider { get; set; } = "OpenAI";
    public string Model { get; set; } = "gpt-4";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string? ApiKey { get; set; }
    public double Temperature { get; set; } = 0.2;
    public int MaxRetries { get; set; } = 3;
    public int MaxTokens { get; set; } = 4000;
    public int TimeoutSeconds { get; set; } = 30;
}
```

## Error Handling

### LLM Connection Errors
- **OpenAI 401**: Invalid API key
  - **Recovery**: Settings → Update API key → Test Connection
- **OpenAI 429**: Rate limit exceeded
  - **Recovery**: Wait, then retry (built-in exponential backoff)
- **Ollama 404**: Model not found or Ollama not running
  - **Recovery**: Ensure Ollama running, pull model (`ollama pull llama2`)

### Validation Errors
- **Missing @Test methods**: Repair prompt asks for test methods
- **No WebDriver teardown**: Repair prompt includes @After cleanup
- **Invalid imports**: Repair prompt specifies required packages

### Fallback Strategy
- If AI generation fails after max retries:
  - System automatically falls back to deterministic generation (Phase 5)
  - User sees deterministic output instead of error
  - Logged as "AI generation failed, using fallback"

## Usage Examples

### OpenAI Configuration
```
Provider: OpenAI
Model: gpt-4
Base URL: https://api.openai.com/v1
API Key: sk-...
Temperature: 0.2
Max Retries: 3
Max Tokens: 4000
```

### Ollama Configuration
```
Provider: Ollama
Model: llama2
Base URL: http://localhost:11434
Temperature: 0.2
Max Retries: 3
Max Tokens: 4000
```
(No API key required for local Ollama)

## Dependencies

### NuGet Packages (New)
- `Microsoft.Extensions.Http` (10.0.3) - HttpClient factory

### Existing Dependencies
- `Microsoft.Extensions.DependencyInjection` - Service registration
- `Microsoft.Extensions.Logging` - Logging
- `System.Text.Json` - JSON parsing
- `System.Net.Http` - HTTP client

## Testing Checklist

- [ ] Settings window opens and loads current configuration
- [ ] API key securely stored and retrieved
- [ ] Test Connection button validates OpenAI API key
- [ ] Test Connection button validates Ollama endpoint
- [ ] AI radio button enables AI generation
- [ ] Generate button uses AICoordinator when AI mode selected
- [ ] Deterministic mode still works (Phase 5 fallback)
- [ ] Invalid code triggers retry loop
- [ ] Max retries exhaustion triggers deterministic fallback
- [ ] Generated code compiles and runs in test framework
- [ ] Status messages display progress and errors

## Troubleshooting

### "Invalid API Key" Error
1. Verify key format: `sk-...` for OpenAI
2. Check key permissions in OpenAI dashboard
3. Verify key hasn't expired
4. Use Settings → Test Connection to validate

### "Connection Timeout"
1. Check internet connectivity
2. Verify base URL is correct
3. Check firewall rules
4. Increase timeout in Settings (advanced)

### "Invalid Java Code Generated"
1. Check code in output panel
2. Look for validation errors in status
3. Check LLM temperature (too high = less deterministic)
4. Try increasing Max Retries in Settings
5. Switch to Deterministic mode as workaround

### Ollama Model Not Found
1. Verify Ollama is running: `ollama serve`
2. List available models: `ollama list`
3. Pull desired model: `ollama pull llama2`
4. Verify Base URL: `http://localhost:11434`

## Next Steps (Phase 7+)

- [ ] Prompt optimization and few-shot examples
- [ ] Multi-language framework support beyond Selenium
- [ ] Model benchmarking (GPT-4 vs GPT-3.5 vs Ollama)
- [ ] Cost tracking for OpenAI usage
- [ ] Async batch generation
- [ ] Version control integration for generated scripts
