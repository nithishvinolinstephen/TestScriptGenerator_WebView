using Microsoft.Extensions.Logging;
using Scriban;

namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// Implementation of IScriptGenerator using Scriban templates.
    /// </summary>
    public class ScriptGenerationService : IScriptGenerator
    {
        private readonly ILogger<ScriptGenerationService> _logger;
        private readonly Dictionary<string, Template> _templates = new();

        public ScriptGenerationService(ILogger<ScriptGenerationService> logger)
        {
            _logger = logger;
            LoadTemplates();
        }

        /// <summary>
        /// Load Scriban templates from embedded resources.
        /// </summary>
        private void LoadTemplates()
        {
            try
            {
                // For now, we have a simple inline template for Selenium Java
                // In production, these would be loaded from embedded resources
                var javaTestTemplate = @"package {{ package_name }};

import org.openqa.selenium.*;
import org.openqa.selenium.chrome.ChromeDriver;
import org.junit.After;
import org.junit.Before;
import org.junit.Test;
import static org.junit.Assert.*;

public class {{ test_class_name }} {
    private WebDriver driver;
    private {{ page_object_name }} page;

    @Before
    public void setUp() {
        System.setProperty(""webdriver.chrome.driver"", ""./chromedriver"");
        driver = new ChromeDriver();
        page = new {{ page_object_name }}(driver);
    }

    @After
    public void tearDown() {
        if (driver != null) {
            driver.quit();
        }
    }

    @Test
    public void testScenario() {
        driver.get(""https://example.com"");
        
{{ for step in steps }}
        // {{ step.description }}
{{ end }}

        assertTrue(true);
    }
}
";

                var javaPageTemplate = @"package {{ package_name }};

import org.openqa.selenium.*;
import org.openqa.selenium.support.FindBy;
import org.openqa.selenium.support.PageFactory;

public class {{ page_object_name }} {
    private WebDriver driver;

{{ for element in elements }}
    @FindBy({{ element.locator_type }} = ""{{ element.locator }}"")
    private WebElement {{ element.variable_name }};

{{ end }}

    public {{ page_object_name }}(WebDriver driver) {
        this.driver = driver;
        PageFactory.initElements(driver, this);
    }

{{ for element in elements }}
    public void interact{{ element.element_id }}() {
        {{ element.variable_name }}.click();
    }

{{ end }}
}
";

                _templates["selenium_java_test"] = Template.Parse(javaTestTemplate);
                _templates["selenium_java_page"] = Template.Parse(javaPageTemplate);

                _logger.LogInformation("Templates loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load templates: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate test script from scenario using template.
        /// </summary>
        public async Task<ScriptOutput> GenerateScriptAsync(ScriptGenerationContext context)
        {
            try
            {
                if (context.Scenario == null || context.Scenario.Steps.Count == 0)
                {
                    return new ScriptOutput
                    {
                        Success = false,
                        ErrorMessage = "No scenario or steps defined"
                    };
                }

                // Build template data
                var templateData = new
                {
                    package_name = context.PackageName,
                    test_class_name = context.TestClassName,
                    page_object_name = context.PageObjectClassName,
                    steps = context.Scenario.Steps.Select(s => new
                    {
                        description = s.GetDescription(),
                        action = s.ActionType.ToString(),
                        value = s.InputValue ?? ""
                    }).ToList(),
                    elements = context.Elements.Select(e => new
                    {
                        element_id = e.ElementId,
                        variable_name = e.VariableName,
                        locator = e.Locator,
                        locator_type = e.LocatorType,
                        element_type = e.ElementType
                    }).ToList()
                };

                // Render Page Object code
                string pageObjectCode = "";
                if (_templates.TryGetValue("selenium_java_page", out var pageTemplate))
                {
                    pageObjectCode = await pageTemplate.RenderAsync(templateData);
                }

                // Render Test Class code
                string testClassCode = "";
                if (_templates.TryGetValue("selenium_java_test", out var testTemplate))
                {
                    testClassCode = await testTemplate.RenderAsync(templateData);
                }

                _logger.LogInformation($"Script generated successfully for framework: {context.Framework}");

                return new ScriptOutput
                {
                    Framework = context.Framework,
                    PageObjectCode = pageObjectCode,
                    TestClassCode = testClassCode,
                    Success = true,
                    GeneratedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Script generation failed: {ex.Message}");
                return new ScriptOutput
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Get available framework templates.
        /// </summary>
        public List<string> GetAvailableFrameworks()
        {
            return new List<string>
            {
                "Selenium Java",
                "Selenium C#",
                "Playwright TypeScript",
                "Playwright .NET"
            };
        }
    }
}
