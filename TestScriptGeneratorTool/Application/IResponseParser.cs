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
                _logger.LogDebug($"Parsing response of {response.Length} characters");
                _logger.LogDebug($"Page Object class name: {pageObjectClassName}");
                _logger.LogDebug($"Test class name: {testClassName}");

                var output = new ScriptOutput { Success = false, ErrorMessage = "No code blocks found" };

                // Extract all code blocks
                var codeBlocks = ExtractCodeBlocks(response);

                if (codeBlocks.Count == 0)
                {
                    _logger.LogWarning("No fenced code blocks found in response");
                    _logger.LogDebug($"Raw response preview: {response.Substring(0, Math.Min(200, response.Length))}...");
                    return output;
                }

                _logger.LogInformation($"Successfully extracted {codeBlocks.Count} code blocks");

                // Classify code blocks by class name
                string? pageObjectCode = null;
                string? testCode = null;

                foreach (var block in codeBlocks)
                {
                    if (ClassifyCodeBlock(block, pageObjectClassName))
                    {
                        pageObjectCode = block;
                        _logger.LogInformation($"Block #{codeBlocks.IndexOf(block) + 1}: Classified as Page Object ({pageObjectClassName})");
                    }
                    else if (ClassifyCodeBlock(block, testClassName))
                    {
                        testCode = block;
                        _logger.LogInformation($"Block #{codeBlocks.IndexOf(block) + 1}: Classified as Test class ({testClassName})");
                    }
                    else
                    {
                        _logger.LogDebug($"Block #{codeBlocks.IndexOf(block) + 1}: Could not classify (size: {block.Length} chars)");
                    }
                }

                // If we found at least the test class, consider it a partial success
                if (!string.IsNullOrEmpty(testCode))
                {
                    output.PageObjectCode = pageObjectCode ?? GeneratePlaceholderPageObject(pageObjectClassName);
                    output.TestClassCode = testCode;
                    output.Success = true;
                    output.ErrorMessage = null;
                    _logger.LogInformation("Response parsed successfully - Test class found");
                }
                else if (codeBlocks.Count >= 2)
                {
                    // Fallback: When we have 2+ blocks but couldn't classify, 
                    // assume first block is Page Object and second is Test class
                    // This handles cases where the AI response doesn't use expected annotations/patterns
                    pageObjectCode = pageObjectCode ?? codeBlocks[0];
                    testCode = codeBlocks[1];
                    
                    output.PageObjectCode = pageObjectCode;
                    output.TestClassCode = testCode;
                    output.Success = true;
                    output.ErrorMessage = null;
                    _logger.LogInformation($"Response parsed with positional fallback - using blocks 1 and 2 (sizes: {codeBlocks[0].Length}, {codeBlocks[1].Length} chars)");
                }
                else if (codeBlocks.Count == 1)
                {
                    // Only one block - try to use it as test code if it looks like test code
                    if (codeBlocks[0].Contains("public void") || codeBlocks[0].Contains("@Test") || 
                        codeBlocks[0].Contains("async") || codeBlocks[0].Contains("describe"))
                    {
                        testCode = codeBlocks[0];
                        output.TestClassCode = testCode;
                        output.PageObjectCode = GeneratePlaceholderPageObject(pageObjectClassName);
                        output.Success = true;
                        output.ErrorMessage = null;
                        _logger.LogInformation("Response parsed with single block as test class");
                    }
                    else
                    {
                        // Single block doesn't look like test code
                        output.ErrorMessage = "Only one code block found and it doesn't appear to be a test class";
                        _logger.LogWarning("Single code block doesn't appear to be test class");
                    }
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

            _logger.LogInformation($"Regex found {matches.Count} code blocks in response");

            foreach (Match match in matches)
            {
                var codeBlock = match.Groups[2].Value.Trim();
                if (!string.IsNullOrEmpty(codeBlock))
                {
                    codeBlocks.Add(codeBlock);
                    _logger.LogDebug($"Extracted code block #{codeBlocks.Count} of length {codeBlock.Length}");
                    _logger.LogDebug($"First 100 chars: {codeBlock.Substring(0, Math.Min(100, codeBlock.Length))}...");
                }
            }

            _logger.LogInformation($"Total code blocks extracted: {codeBlocks.Count}");
            
            // If no code blocks found with fence, try alternative patterns
            if (codeBlocks.Count == 0)
            {
                _logger.LogWarning("No fenced code blocks found, attempting alternative parsing");
                _logger.LogWarning($"Raw response length: {response.Length}, first 300 chars: {response.Substring(0, Math.Min(300, response.Length))}");
                
                // Try finding code blocks without explicit fences (fallback)
                var altPattern = @"(?:public class|package|import|function|const\s+\w+\s*=|def\s+\w+)";
                if (Regex.IsMatch(response, altPattern))
                {
                    _logger.LogInformation("Found code-like content, extracting class definitions");
                    // If response contains code-like content, try to extract class definitions
                    var classPattern = @"(?:public\s+)?(?:static\s+)?class\s+\w+[\s\S]*?(?=\n\s*(?:public\s+)?(?:static\s+)?class|\npackage|\n\n\n|$)";
                    var classMatches = Regex.Matches(response, classPattern, RegexOptions.Multiline);
                    foreach (Match match in classMatches)
                    {
                        var codeBlock = match.Value.Trim();
                        if (!string.IsNullOrEmpty(codeBlock) && codeBlock.Length > 50)
                        {
                            codeBlocks.Add(codeBlock);
                            _logger.LogInformation($"Extracted class definition #{codeBlocks.Count} of length {codeBlock.Length}");
                        }
                    }
                }
            }

            _logger.LogInformation($"Final code block count: {codeBlocks.Count}");
            if (codeBlocks.Count > 0)
            {
                _logger.LogDebug($"Block sizes: {string.Join(", ", codeBlocks.Select(b => b.Length))} chars");
            }
            
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
                // Check for locator patterns, element interactions, page object methods
                return Regex.IsMatch(code, @"(?:private|public|protected)\s+(?:(?:final|static)\s+)?(?:WebDriver|By|WebElement|IPage|ILocator|@FindBy|@CacheLookup)", RegexOptions.IgnoreCase);
            }
            else if (targetClassName.Contains("Test"))
            {
                // This is the Test class - look for test annotations or test methods
                // More comprehensive: look for test methods, setup/teardown, driver initialization
                return Regex.IsMatch(code, @"(?:@Test|@BeforeEach|@AfterEach|@Before|@After|\[Test\]|\[SetUp\]|\[TearDown\]|async\s+function\s+test|function\s+test|void\s+test|public\s+void\s+test)", RegexOptions.IgnoreCase)
                    || Regex.IsMatch(code, @"(?:setUp|tearDown|@Before|@After)", RegexOptions.IgnoreCase)
                    || Regex.IsMatch(code, @"(?:\.quit\(\)|driver\s*=\s*new|WebDriver\s+driver|IWebDriver\s+driver|\.close\(\)|\.quit\(\))", RegexOptions.IgnoreCase);
            }
            else
            {
                // Fallback: check for explicit class definition
                var pattern = $@"(?:public\s+)?class\s+{Regex.Escape(targetClassName)}\s*(?:extends|implements|{{)";
                return Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase);
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
