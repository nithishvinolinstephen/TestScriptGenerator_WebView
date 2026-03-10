# ICE POT Quick Reference

## What Changed?

### 1. New File: `ICEPOTPrompts.cs`
Contains framework-specific prompts using ICE POT format.

**Four methods**:
- `GetSeleniumJavaPrompt(elements, userPrompt, url)`
- `GetSeleniumCSharpPrompt(elements, userPrompt, url)`
- `GetPlaywrightTypeScriptPrompt(elements, userPrompt, url)`
- `GetPlaywrightDotNetPrompt(elements, userPrompt, url)`

### 2. Updated: `IPromptBuilder.cs`
- `BuildGenerationPrompt()`: Routes to framework-specific ICE POT prompt
- `BuildRepairPrompt()`: Restructured with ICE POT format
- `BuildFallbackGenerationPrompt()`: Enhanced with ICE POT structure
- `BuildElementsDescription()`: Formats element metadata consistently

## ICE POT Format Structure

```
═══════════════════════════════════════════════════════════════════════════════
I — INSTRUCTIONS
═══════════════════════════════════════════════════════════════════════════════
[Task definition and hard constraints]

═══════════════════════════════════════════════════════════════════════════════
C — CONTEXT
═══════════════════════════════════════════════════════════════════════════════
[Background, framework details, element metadata]

═══════════════════════════════════════════════════════════════════════════════
E — EXAMPLE
═══════════════════════════════════════════════════════════════════════════════
[Code style reference or template example]

═══════════════════════════════════════════════════════════════════════════════
P — PERSONA
═══════════════════════════════════════════════════════════════════════════════
[AI role and expertise definition]

═══════════════════════════════════════════════════════════════════════════════
O — OUTPUT FORMAT
═══════════════════════════════════════════════════════════════════════════════
[Exact output structure and code fence requirements]

═══════════════════════════════════════════════════════════════════════════════
T — TONE
═══════════════════════════════════════════════════════════════════════════════
[Communication style: direct, professional, etc.]
```

## Framework Routing

When `BuildGenerationPrompt()` is called with a context:

```
User selects framework
         ↓
Detects context.Framework
         ↓
Routes to appropriate method:
├─ "Selenium Java"        → ICEPOTPrompts.GetSeleniumJavaPrompt()
├─ "Selenium C#"          → ICEPOTPrompts.GetSeleniumCSharpPrompt()
├─ "Playwright TypeScript"→ ICEPOTPrompts.GetPlaywrightTypeScriptPrompt()
├─ "Playwright .NET"      → ICEPOTPrompts.GetPlaywrightDotNetPrompt()
└─ Default               → GetSeleniumJavaPrompt()
         ↓
Returns framework-specific ICE POT formatted prompt
         ↓
Sent to LLM client
```

## Error Handling

```
BuildGenerationPrompt()
    ├─ Try: Build ICE POT prompt
    │  └─ Success: Return framework-specific prompt
    │
    └─ Catch Exception:
       └─ Log error
       └─ Return BuildFallbackGenerationPrompt()
```

## Element Metadata Format

Input (from ScriptGenerationContext):
```csharp
var elements = new List<ElementWithLocator> {
    new() { 
        ElementId = "btn1",
        ElementType = "button",
        Locator = "#submit-btn",
        LocatorType = "css",
        VariableName = "submitButton"
    },
    new() { 
        ElementId = "inp1",
        ElementType = "input",
        Locator = "input[name='email']",
        LocatorType = "css",
        VariableName = "emailInput"
    }
};
```

Output (BuildElementsDescription result):
```
Element 1:
  - Type: button
  - Locator: #submit-btn
  - Variable: submitButton
Element 2:
  - Type: input
  - Locator: input[name='email']
  - Variable: emailInput
```

## Key Constraints in All Prompts

1. **Return ONLY code** - No explanations outside code blocks
2. **Exactly TWO code blocks** - Page Object class, then Test class
3. **Language-specific fences** - ` ```java ` / ` ```csharp ` / ` ```typescript `
4. **No Thread.Sleep()** - Use explicit waits only
5. **Page Object Model** - Required pattern
6. **All imports included** - Complete, runnable code
7. **Actual element values** - Use provided locators

## Repair Prompt

When validation fails, `BuildRepairPrompt()` is called:

```
REPAIR REQUEST — ICE POT Format
[lists all failures]
INSTRUCTIONS: Fix identified issues
CONTEXT: Framework, URL, elements, user requirement
OUTPUT: Two code blocks with fixes
```

## Testing the Implementation

### Quick Test
1. Open the application
2. Select a framework (e.g., "Selenium Java")
3. Add test elements
4. Click "Generate Code"
5. Verify prompt uses framework-specific guidance

### Debug Test
1. Check ScriptGenerationService debug log
2. Look for: "Built ICE POT generation prompt for [Framework]"
3. Compare output against ICE POT structure

### Fallback Test
1. Simulate error in ICEPOTPrompts by editing file
2. Observe fallback prompt is used
3. Verify logging shows: "Error building prompt... using fallback"

## Files to Review

1. **ICEPOTPrompts.cs** (622 lines)
   - Framework-specific implementations
   - Each prompt ~150 lines

2. **IPromptBuilder.cs** (234 lines)
   - Main routing logic
   - Fallback handling
   - Helper methods

3. **ICEPOT_IMPLEMENTATION.md**
   - Comprehensive documentation
   - Benefits and features
   - Future enhancements

## No Breaking Changes

- ✅ IPromptBuilder interface unchanged
- ✅ Existing consumers work as-is
- ✅ No new dependencies
- ✅ Backward compatible

## Next Steps

1. **Testing**: Run unit tests on prompt builders
2. **Validation**: Generate code and verify quality
3. **Integration**: Test with actual LLM clients
4. **Monitoring**: Track prompt effectiveness
5. **Refinement**: Adjust prompts based on results

---

**Status**: ✅ Complete and Ready for Testing
