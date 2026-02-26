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

            // Validate Page Object
            ValidatePageObject(pageObjectCode, pageObjectClassName, result);

            // Validate Test Class
            ValidateTestClass(testCode, testClassName, result);

            result.IsValid = result.Failures.Count == 0;

            if (!result.IsValid)
            {
                _logger.LogWarning($"Code validation failed: {string.Join(", ", result.Failures)}");
            }
            else
            {
                _logger.LogInformation("Code validation passed");
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

            // Check for WebDriver field
            if (!Regex.IsMatch(code, @"WebDriver\s+driver"))
            {
                result.Failures.Add("Page Object missing WebDriver driver field");
            }

            // Check for constructor
            if (!Regex.IsMatch(code, $@"public\s+{Regex.Escape(className)}\s*\("))
            {
                result.Failures.Add($"Page Object missing constructor {className}(WebDriver)");
            }

            // Check for PageFactory.initElements
            if (!Regex.IsMatch(code, @"PageFactory\.initElements"))
            {
                result.Failures.Add("Page Object missing PageFactory.initElements call");
            }

            // Check for imports
            if (!Regex.IsMatch(code, @"import\s+org\.openqa\.selenium\.\*"))
            {
                result.Failures.Add("Page Object missing Selenium imports");
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

            // Check for @Test annotation
            if (!Regex.IsMatch(code, @"@Test\s+public\s+void\s+\w+\s*\("))
            {
                result.Failures.Add("Test class missing @Test annotation or test method");
            }

            // Check for @Before
            if (!Regex.IsMatch(code, @"@Before\s+public\s+void"))
            {
                result.Failures.Add("Test class missing @Before setup method");
            }

            // Check for @After
            if (!Regex.IsMatch(code, @"@After\s+public\s+void"))
            {
                result.Failures.Add("Test class missing @After teardown method");
            }

            // Check for driver quit
            if (!Regex.IsMatch(code, @"driver\.quit\s*\(\)"))
            {
                result.Failures.Add("Test class missing driver.quit() in teardown");
            }

            // Check for JUnit imports
            if (!Regex.IsMatch(code, @"import.*junit"))
            {
                result.Failures.Add("Test class missing JUnit imports");
            }

            // Check for WebDriver instance
            if (!Regex.IsMatch(code, @"WebDriver\s+driver"))
            {
                result.Failures.Add("Test class missing WebDriver driver field");
            }
        }
    }
}
