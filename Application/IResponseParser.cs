using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// Service for parsing LLM responses and extracting code blocks.
    /// </summary>
    public interface IResponseParser
    {
        /// <summary>
        /// Parse LLM response to extract Page Object and Test class code.
        /// </summary>
        /// <param name="response">Raw LLM response text.</param>
        /// <param name="pageObjectClassName">Expected Page Object class name.</param>
        /// <param name="testClassName">Expected Test class name.</param>
        /// <returns>Parsed ScriptOutput with extracted code.</returns>
        ScriptOutput ParseResponse(string response, string pageObjectClassName, string testClassName);
    }

    /// <summary>
    /// Implementation of IResponseParser using regex for code block extraction.
    /// </summary>
    public class ResponseParser : IResponseParser
    {
        private readonly ILogger<ResponseParser> _logger;

        public ResponseParser(ILogger<ResponseParser> logger)
        {
            _logger = logger;
        }

        public ScriptOutput ParseResponse(string response, string pageObjectClassName, string testClassName)
        {
            try
            {
                var output = new ScriptOutput { Success = false, ErrorMessage = "No code blocks found" };

                // Extract all code blocks
                var codeBlocks = ExtractCodeBlocks(response);

                if (codeBlocks.Count == 0)
                {
                    _logger.LogWarning("No fenced code blocks found in response");
                    return output;
                }

                // Classify code blocks by class name
                string? pageObjectCode = null;
                string? testCode = null;

                foreach (var block in codeBlocks)
                {
                    if (ClassifyCodeBlock(block, pageObjectClassName))
                        pageObjectCode = block;
                    else if (ClassifyCodeBlock(block, testClassName))
                        testCode = block;
                }

                // If we found at least the test class, consider it a partial success
                if (!string.IsNullOrEmpty(testCode))
                {
                    output.PageObjectCode = pageObjectCode ?? GeneratePlaceholderPageObject(pageObjectClassName);
                    output.TestClassCode = testCode;
                    output.Success = true;
                    output.ErrorMessage = null;
                    _logger.LogInformation("Response parsed successfully");
                }
                else if (codeBlocks.Count > 0)
                {
                    // Fallback: use largest code block as test class
                    var largestBlock = codeBlocks.OrderByDescending(b => b.Length).First();
                    output.TestClassCode = largestBlock;
                    output.PageObjectCode = pageObjectCode ?? GeneratePlaceholderPageObject(pageObjectClassName);
                    output.Success = true;
                    output.ErrorMessage = null;
                    _logger.LogInformation("Response parsed with fallback logic");
                }

                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Response parsing failed: {ex.Message}");
                return new ScriptOutput
                {
                    Success = false,
                    ErrorMessage = $"Parsing error: {ex.Message}"
                };
            }
        }

        private List<string> ExtractCodeBlocks(string response)
        {
            var codeBlocks = new List<string>();

            // Pattern for fenced code blocks: ```[language] ... ```
            // This pattern is more flexible and handles various formatting
            var pattern = @"```(?:(?:java|csharp|c#|typescript|ts|javascript|js|python|go|rust|kotlin)\b)?\s*(?:\[className:\s*([^\]]+)\])?\s*[\r\n]?([\s\S]*?)```";
            var matches = Regex.Matches(response, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                var codeBlock = match.Groups[2].Value.Trim();
                if (!string.IsNullOrEmpty(codeBlock))
                {
                    codeBlocks.Add(codeBlock);
                    _logger.LogDebug($"Extracted code block of length {codeBlock.Length}");
                }
            }

            _logger.LogInformation($"Extracted {codeBlocks.Count} code blocks from response");
            
            // If no code blocks found with fence, try alternative patterns
            if (codeBlocks.Count == 0)
            {
                _logger.LogWarning("No fenced code blocks found, attempting alternative parsing");
                // Try finding code blocks without explicit fences (fallback)
                var altPattern = @"(?:public class|package|import|function|const\s+\w+\s*=|def\s+\w+)";
                if (Regex.IsMatch(response, altPattern))
                {
                    // If response contains code-like content, try to extract class definitions
                    var classPattern = @"(?:public\s+)?(?:static\s+)?class\s+\w+[\s\S]*?(?=\n\s*(?:public\s+)?(?:static\s+)?class|\npackage|\n\n\n|$)";
                    var classMatches = Regex.Matches(response, classPattern, RegexOptions.Multiline);
                    foreach (Match match in classMatches)
                    {
                        var codeBlock = match.Value.Trim();
                        if (!string.IsNullOrEmpty(codeBlock) && codeBlock.Length > 50)
                        {
                            codeBlocks.Add(codeBlock);
                        }
                    }
                }
            }

            _logger.LogInformation($"Total code blocks extracted: {codeBlocks.Count}");
            return codeBlocks;
        }

        private bool ClassifyCodeBlock(string code, string targetClassName)
        {
            // Check if code block contains the class definition or common patterns
            // For Page Object, look for "Page" or "PageObject" in class name
            // For Test class, look for "Test" or test-runner attributes
            
            if (targetClassName.Contains("Page"))
            {
                // This is the Page Object class - look for field declarations and methods
                return Regex.IsMatch(code, @"(?:private|public|protected)\s+(?:final\s+)?(?:WebDriver|By|WebElement|IPage|ILocator)");
            }
            else if (targetClassName.Contains("Test"))
            {
                // This is the Test class - look for test annotations or test methods
                return Regex.IsMatch(code, @"(?:@Test|@BeforeEach|@AfterEach|\[Test\]|test\(|async test\(|describe\(|it\()");
            }
            else
            {
                // Fallback: check for explicit class definition
                var pattern = $@"(?:public\s+)?class\s+{Regex.Escape(targetClassName)}\s*(?:extends|implements|{{)";
                return Regex.IsMatch(code, pattern);
            }
        }

        private string GeneratePlaceholderPageObject(string className)
        {
            return $@"package com.example.automation;

import org.openqa.selenium.*;
import org.openqa.selenium.support.PageFactory;

public class {className} {{
    private WebDriver driver;

    public {className}(WebDriver driver) {{
        this.driver = driver;
        PageFactory.initElements(driver, this);
    }}

    public WebDriver getDriver() {{
        return driver;
    }}
}}
";
        }
    }
}
