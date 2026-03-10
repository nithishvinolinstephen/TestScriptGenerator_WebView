using Microsoft.Extensions.Logging;

namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// Service for building prompts for LLM from test scenarios.
    /// </summary>
    public interface IPromptBuilder
    {
        /// <summary>
        /// Build a prompt for code generation.
        /// </summary>
        /// <param name="context">Script generation context.</param>
        /// <returns>Formatted prompt for LLM.</returns>
        string BuildGenerationPrompt(ScriptGenerationContext context);

        /// <summary>
        /// Build a repair prompt after validation failure.
        /// </summary>
        /// <param name="context">Script generation context.</param>
        /// <param name="failures">List of validation failures.</param>
        /// <returns>Repair prompt for LLM.</returns>
        string BuildRepairPrompt(ScriptGenerationContext context, List<string> failures);
    }

    /// <summary>
    /// Implementation of IPromptBuilder.
    /// </summary>
    public class PromptBuilder : IPromptBuilder
    {
        private readonly ILogger<PromptBuilder> _logger;

        public PromptBuilder(ILogger<PromptBuilder> logger)
        {
            _logger = logger;
        }

        public string BuildGenerationPrompt(ScriptGenerationContext context)
        {
            try
            {
                // Build element metadata description
                var elementsText = BuildElementsDescription(context.Elements);
                _logger.LogInformation($"Built elements description with {context.Elements.Count} elements");
                _logger.LogDebug($"Elements text: {elementsText}");
                
                // Get ICE POT formatted prompt based on framework
                var iceotPrompt = context.Framework switch
                {
                    "Selenium Java" => ICEPOTPrompts.GetSeleniumJavaPrompt(elementsText, context.UserPrompt, context.ApplicationUrl),
                    "Selenium C#" => ICEPOTPrompts.GetSeleniumCSharpPrompt(elementsText, context.UserPrompt, context.ApplicationUrl),
                    "Playwright TypeScript" => ICEPOTPrompts.GetPlaywrightTypeScriptPrompt(elementsText, context.UserPrompt, context.ApplicationUrl),
                    "Playwright .NET" => ICEPOTPrompts.GetPlaywrightDotNetPrompt(elementsText, context.UserPrompt, context.ApplicationUrl),
                    _ => ICEPOTPrompts.GetSeleniumJavaPrompt(elementsText, context.UserPrompt, context.ApplicationUrl)
                };

                _logger.LogInformation($"Built ICE POT generation prompt for {context.Framework} with {iceotPrompt.Length} characters");
                _logger.LogDebug($"Prompt preview: {iceotPrompt.Substring(0, Math.Min(200, iceotPrompt.Length))}...");
                return iceotPrompt;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error building prompt: {ex.Message}, using fallback");
                return BuildFallbackGenerationPrompt(context);
            }
        }

        public string BuildRepairPrompt(ScriptGenerationContext context, List<string> failures)
        {
            var failureText = string.Join("\n", failures.Select(f => $"  • {f}"));
            var elementsText = BuildElementsDescription(context.Elements);

            var repairPrompt = $@"
═══════════════════════════════════════════════════════════════════════════════
REPAIR REQUEST — ICE POT Format
═══════════════════════════════════════════════════════════════════════════════

PREVIOUS GENERATION HAD VALIDATION FAILURES:
{failureText}

INSTRUCTIONS:
Regenerate the {context.Framework} test code fixing ALL identified issues above.

CONTEXT:
Framework: {context.Framework}
Application URL: {context.ApplicationUrl}
Element Metadata:
{elementsText}

USER REQUIREMENT:
{context.UserPrompt}

OUTPUT:
Generate EXACTLY TWO code blocks:
1. Page Object class
2. Test class

Both must be complete, runnable, and fix all validation failures.
Return ONLY code in proper code fences - no explanations.

═══════════════════════════════════════════════════════════════════════════════
REGENERATE THE CODE NOW:
";

            _logger.LogInformation($"Built repair prompt for {context.Framework}");
            return repairPrompt;
        }

        private string LoadPromptTemplate(string framework, string templateName)
        {
            try
            {
                // Map framework names to folder names
                var frameworkFolder = framework switch
                {
                    "Selenium Java" => "SeleniumJava",
                    "Selenium C#" => "SeleniumCSharp",
                    "Playwright TypeScript" => "PlaywrightTypeScript",
                    "Playwright .NET" => "PlaywrightDotNet",
                    _ => "SeleniumJava"
                };

                // Try to load from embedded resources
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = $"TestScriptGeneratorTool.Prompts.{frameworkFolder}.{templateName}";

                _logger.LogInformation($"Attempting to load resource: {resourceName}");

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        // List available resources for debugging
                        var allResources = assembly.GetManifestResourceNames()
                            .Where(r => r.Contains("Prompts"))
                            .ToList();
                        
                        _logger.LogWarning($"Resource not found: {resourceName}. Available prompt resources: {string.Join(", ", allResources)}");
                        
                        // Return fallback text
                        return $"Framework: {framework}\nPlease provide test elements and steps.";
                    }

                    using (var reader = new System.IO.StreamReader(stream))
                    {
                        var content = reader.ReadToEnd();
                        _logger.LogInformation($"Successfully loaded template: {resourceName}");
                        return content;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading prompt template: {ex.Message}");
                return $"Framework: {framework}\nPlease provide test elements and steps.";
            }
        }

        private string BuildFallbackGenerationPrompt(ScriptGenerationContext context)
        {
            var language = context.Framework switch
            {
                "Selenium Java" => "java",
                "Selenium C#" => "csharp",
                "Playwright TypeScript" => "typescript",
                "Playwright .NET" => "csharp",
                _ => "java"
            };

            var prompt = $@"You are an expert test automation engineer. Generate production-ready automation test code.

CRITICAL RULES:
1. Return ONLY code. No explanations, no commentary.
2. Generate exactly TWO code blocks:
   - First block: Page Object class (```{language} ... ```)
   - Second block: Test class (```{language} ... ```)
3. Use Page Object Model pattern.
4. Include explicit waits (no Thread.Sleep).
5. Use actual values from element metadata.
6. Wrap in proper language fence.
7. Include all necessary imports.

Framework: {context.Framework}
Language: {language}
Application URL: {context.ApplicationUrl}

Elements to interact with:
{BuildElementsDescription(context.Elements)}

User's test scenario:
{context.UserPrompt}

Generate the complete, runnable code with proper code fences now:";

            return prompt;
        }

        private string BuildElementsDescription(List<ElementWithLocator> elements)
        {
            if (elements.Count == 0)
                return "No elements defined";

            var lines = new List<string>();
            int count = 1;
            foreach (var element in elements)
            {
                lines.Add($"Element {count}:");
                lines.Add($"  - Type: {element.ElementType}");
                lines.Add($"  - Locator: {element.Locator}");
                lines.Add($"  - Variable: {element.VariableName}");
                count++;
            }

            return string.Join("\n", lines);
        }

        private string BuildStepsDescription(TestScenario? scenario)
        {
            if (scenario?.Steps == null || scenario.Steps.Count == 0)
            {
                return "No specific steps defined - generate test methods that interact with the provided elements";
            }

            var lines = new List<string>();
            for (int i = 0; i < scenario.Steps.Count; i++)
            {
                var step = scenario.Steps[i];
                var description = step.GetDescription();
                if (!string.IsNullOrEmpty(description))
                    lines.Add($"Step {i + 1}: {description}");
            }

            return lines.Count > 0 ? string.Join("\n", lines) : "Execute interactions with provided elements";
        }
    }
}
