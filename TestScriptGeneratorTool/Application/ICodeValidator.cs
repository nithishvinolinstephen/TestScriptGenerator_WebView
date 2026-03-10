using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// Result of code validation.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Failures { get; set; } = new();

        public override string ToString()
        {
            return IsValid ? "Valid" : $"Invalid ({Failures.Count} failures)";
        }
    }

    /// <summary>
    /// Service for validating generated test code.
    /// </summary>
    public interface ICodeValidator
    {
        /// <summary>
        /// Validate generated Page Object and Test class code.
        /// </summary>
        /// <param name="pageObjectCode">Page Object class code.</param>
        /// <param name="testCode">Test class code.</param>
        /// <param name="pageObjectClassName">Expected Page Object class name.</param>
        /// <param name="testClassName">Expected Test class name.</param>
        /// <returns>Validation result with list of failures.</returns>
        ValidationResult ValidateCode(string pageObjectCode, string testCode, 
            string pageObjectClassName, string testClassName);
    }

    /// <summary>
    /// Implementation of ICodeValidator with heuristic checks.
    /// </summary>
    public class CodeValidator : ICodeValidator
    {
        private readonly ILogger<CodeValidator> _logger;

        public CodeValidator(ILogger<CodeValidator> logger)
        {
            _logger = logger;
        }

        public ValidationResult ValidateCode(string pageObjectCode, string testCode,
            string pageObjectClassName, string testClassName)
        {
            var result = new ValidationResult { IsValid = true };

            _logger.LogDebug($"Validating code: PageObject={pageObjectClassName}, Test={testClassName}");
            _logger.LogDebug($"PageObject code length: {pageObjectCode?.Length ?? 0}, Test code length: {testCode?.Length ?? 0}");

            // Validate Page Object
            ValidatePageObject(pageObjectCode, pageObjectClassName, result);

            // Validate Test Class
            ValidateTestClass(testCode, testClassName, result);

            result.IsValid = result.Failures.Count == 0;

            if (!result.IsValid)
            {
                _logger.LogWarning($"Code validation failed with {result.Failures.Count} failures: {string.Join(", ", result.Failures)}");
            }
            else
            {
                _logger.LogInformation("Code validation passed - all checks OK");
            }

            return result;
        }

        private void ValidatePageObject(string code, string className, ValidationResult result)
        {
            if (string.IsNullOrEmpty(code))
            {
                result.Failures.Add($"Page Object class '{className}' is empty");
                return;
            }

            // Check for class declaration
            if (!Regex.IsMatch(code, $@"(?:public\s+)?class\s+{Regex.Escape(className)}"))
            {
                result.Failures.Add($"Page Object class '{className}' declaration not found");
            }

            // Check for WebDriver field OR driver initialization
            if (!Regex.IsMatch(code, @"(?:WebDriver|IWebDriver|driver)\s+(?:driver|_driver)"))
            {
                result.Failures.Add("Page Object missing driver field");
            }

            // Check for constructor - more lenient pattern
            if (!Regex.IsMatch(code, $@"(?:public|private)\s+{Regex.Escape(className)}\s*\("))
            {
                result.Failures.Add($"Page Object missing constructor");
            }

            // Check for imports - more lenient
            if (!Regex.IsMatch(code, @"(?:import|using)\s+(?:org\.openqa|OpenQA|Playwright)"))
            {
                result.Failures.Add("Page Object missing proper imports");
            }
        }

        private void ValidateTestClass(string code, string className, ValidationResult result)
        {
            if (string.IsNullOrEmpty(code))
            {
                result.Failures.Add($"Test class '{className}' is empty");
                return;
            }

            // Check for class declaration
            if (!Regex.IsMatch(code, $@"(?:public\s+)?class\s+{Regex.Escape(className)}"))
            {
                result.Failures.Add($"Test class '{className}' declaration not found");
            }

            // Check for @Test or test method annotation - more lenient
            if (!Regex.IsMatch(code, @"@(?:Test|test)\s*(?:\(\)|public)", RegexOptions.IgnoreCase))
            {
                // Alternative: check if there's any method that looks like a test
                if (!Regex.IsMatch(code, @"(?:public|void)\s+(?:test|it|should)\w*\s*\(", RegexOptions.IgnoreCase))
                {
                    result.Failures.Add("Test class missing @Test annotation or test method");
                }
            }

            // Check for setup/teardown - more lenient (JUnit4 or JUnit5)
            if (!Regex.IsMatch(code, @"@(?:Before|BeforeEach|Setup|BeforeClass)\s*(?:\(\)|public)", RegexOptions.IgnoreCase))
            {
                // Don't fail if missing - some tests might not need setup
                _logger.LogInformation("Test class missing @Before/@BeforeEach annotation");
            }

            // Check for cleanup - more lenient
            if (!Regex.IsMatch(code, @"@(?:After|AfterEach|TearDown|AfterClass)\s*(?:\(\)|public)", RegexOptions.IgnoreCase))
            {
                // Check if driver.quit() is called directly in test
                if (!Regex.IsMatch(code, @"(?:driver|webDriver)\.quit\s*\(\)", RegexOptions.IgnoreCase))
                {
                    _logger.LogInformation("Test class missing @After/@AfterEach or driver.quit()");
                }
            }

            // Check for driver initialization OR WebDriver - more lenient
            if (!Regex.IsMatch(code, @"(?:WebDriver|IWebDriver|driver)\s+(?:driver|_driver|webDriver)", RegexOptions.IgnoreCase))
            {
                if (!Regex.IsMatch(code, @"new\s+ChromeDriver\(\)", RegexOptions.IgnoreCase))
                {
                    result.Failures.Add("Test class missing WebDriver initialization");
                }
            }
        }
    }
}
