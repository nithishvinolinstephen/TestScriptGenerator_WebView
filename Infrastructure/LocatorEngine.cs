using Microsoft.Extensions.Logging;
using TestScriptGeneratorTool.Domain;

namespace TestScriptGeneratorTool.Infrastructure
{
    /// <summary>
    /// Implementation of locator engine for element identification.
    /// Follows priority: ID → Name → Data-QA → CSS Selector → XPath
    /// </summary>
    public class LocatorEngine : ILocatorEngine
    {
        private readonly ILogger<LocatorEngine> _logger;
        private readonly WebViewService _webViewService;

        public LocatorEngine(ILogger<LocatorEngine> logger, WebViewService webViewService)
        {
            _logger = logger;
            _webViewService = webViewService;
        }

        /// <summary>
        /// Generates a locator definition by applying priority rules.
        /// Priority: ID → Name → Data-QA → CSS Selector → XPath
        /// </summary>
        public async Task<LocatorDefinition> GenerateLocatorAsync(ElementDescriptor element, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating locator for element: {TagName}", element.TagName);

                var locator = new LocatorDefinition();
                var alternatives = GetAlternativeLocators(element);

                // Apply priority rules
                if (!string.IsNullOrEmpty(element.Id))
                {
                    locator.PrimaryLocator = $"#{element.Id}";
                    locator.LocatorType = "id";
                    _logger.LogInformation("Primary locator is ID: {Locator}", locator.PrimaryLocator);
                }
                else if (!string.IsNullOrEmpty(element.Name))
                {
                    locator.PrimaryLocator = $"[name='{element.Name}']";
                    locator.LocatorType = "name";
                    _logger.LogInformation("Primary locator is Name: {Locator}", locator.PrimaryLocator);
                }
                else if (element.Attributes.ContainsKey("data-qa"))
                {
                    var dataQa = element.Attributes["data-qa"];
                    locator.PrimaryLocator = $"[data-qa='{dataQa}']";
                    locator.LocatorType = "data-qa";
                    _logger.LogInformation("Primary locator is Data-QA: {Locator}", locator.PrimaryLocator);
                }
                else if (!string.IsNullOrEmpty(element.CssSelector))
                {
                    locator.PrimaryLocator = element.CssSelector;
                    locator.LocatorType = "css";
                    _logger.LogInformation("Primary locator is CSS: {Locator}", locator.PrimaryLocator);
                }
                else if (!string.IsNullOrEmpty(element.XPath))
                {
                    locator.PrimaryLocator = element.XPath;
                    locator.LocatorType = "xpath";
                    _logger.LogInformation("Primary locator is XPath: {Locator}", locator.PrimaryLocator);
                }

                // Validate primary locator uniqueness
                if (!string.IsNullOrEmpty(locator.PrimaryLocator))
                {
                    var isUnique = await ValidateLocatorUniquenessAsync(locator.PrimaryLocator, cancellationToken);
                    if (isUnique)
                    {
                        _logger.LogInformation("Primary locator is unique");
                    }
                    else
                    {
                        _logger.LogWarning("Primary locator is not unique: {Locator}", locator.PrimaryLocator);
                    }
                }

                // Add alternatives
                locator.Alternatives = alternatives.Where(alt => alt != locator.PrimaryLocator).ToList();
                _logger.LogInformation("Generated {Count} alternative locators", locator.Alternatives.Count);

                return locator;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to generate locator: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates that a locator uniquely identifies one element in the DOM.
        /// </summary>
        public async Task<bool> ValidateLocatorUniquenessAsync(string locator, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Validating locator uniqueness: {Locator}", locator);

                // Execute script to count elements matching the locator
                var script = $@"
(function() {{
    try {{
        var elements = document.querySelectorAll('{EscapeForJavaScript(locator)}');
        return elements.length;
    }} catch(e) {{
        return -1;
    }}
}})();
";
                var result = await _webViewService.ExecuteScriptAsync(script);

                if (int.TryParse(result, out int count))
                {
                    var isUnique = count == 1;
                    _logger.LogInformation("Locator validation result: {Count} matching elements, Unique: {IsUnique}", count, isUnique);
                    return isUnique;
                }

                _logger.LogWarning("Failed to parse locator validation result: {Result}", result);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to validate locator uniqueness: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all alternative locators for an element in priority order.
        /// </summary>
        public List<string> GetAlternativeLocators(ElementDescriptor element)
        {
            var alternatives = new List<string>();

            // ID locator
            if (!string.IsNullOrEmpty(element.Id))
            {
                alternatives.Add($"#{element.Id}");
            }

            // Name locator
            if (!string.IsNullOrEmpty(element.Name))
            {
                alternatives.Add($"[name='{element.Name}']");
            }

            // Data-QA locator
            if (element.Attributes.ContainsKey("data-qa"))
            {
                alternatives.Add($"[data-qa='{element.Attributes["data-qa"]}']");
            }

            // Data-TestID locator
            if (element.Attributes.ContainsKey("data-testid"))
            {
                alternatives.Add($"[data-testid='{element.Attributes["data-testid"]}']");
            }

            // CSS selector
            if (!string.IsNullOrEmpty(element.CssSelector))
            {
                alternatives.Add(element.CssSelector);
            }

            // XPath
            if (!string.IsNullOrEmpty(element.XPath))
            {
                alternatives.Add(element.XPath);
            }

            // Tag + class combination
            if (element.ClassList.Count > 0)
            {
                var classes = string.Join(".", element.ClassList);
                alternatives.Add($"{element.TagName}.{classes}");
            }

            _logger.LogInformation("Generated {Count} alternative locators", alternatives.Count);
            return alternatives.Distinct().ToList();
        }

        /// <summary>
        /// Escapes special characters in locators for JavaScript execution.
        /// </summary>
        private string EscapeForJavaScript(string locator)
        {
            return locator.Replace("'", "\\'").Replace("\"", "\\\"");
        }
    }
}
