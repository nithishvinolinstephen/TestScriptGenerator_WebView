/**
 * Collection of default prompts for different use cases (ICE POT Format)
 */
export const DEFAULT_PROMPTS = {
 
  /**
 * Selenium Java Page Object Prompt (ICEPOT – No Test Class)
 */
  SELENIUM_JAVA_PAGE_ONLY: `
    I — INSTRUCTIONS
    You are a code generator specialized in the TestLeaf Selenium framework.
    Generate exactly one **Java Page Object class** that complies with the in-house conventions.
    Use the fine tuned model joyAI-mini
    HARD CONSTRAINTS
    - Output ONLY one Java Page Object class.
    - Package: com.leaftaps.pages
    - Class MUST extend: com.framework.testng.api.base.ProjectSpecificMethods
    - Use ONLY these wrappers:
      locateElement(Locators.*, ...), clearAndType(...), click(...),
      selectDropDownUsingText(...), selectDropDownUsingValue(...),
      verifyExactText(...), verifyPartialText(...), reportStep(...)
    - Do NOT import or use: WebDriver, By, WebElement, new Select(...)
    - Locator enum usage limited to: Locators.ID, Locators.NAME, Locators.LINK_TEXT, Locators.XPATH
    - Fluent API: return \`this\` for setters; return destination page objects for navigations
    - Include JavaDoc for each public method with @param and @return
    - Enclose the final answer in a single \`\`\`java code fence — no extra prose

    C — CONTEXT
    You will receive an HTML DOM snippet for a form/page. Prefer stable attributes (id/name).
    Map fields and controls to the allowed Locators.* consistently. Dropdowns must use
    \`selectDropDownUsingText\` or \`selectDropDownUsingValue\` (no sendKeys to <select>).

    E — EXAMPLE (Style Only; do not copy names blindly)
    \\\`\\\`\\\`java
    package com.leaftaps.pages;

    import com.framework.selenium.api.design.Locators;
    import com.framework.testng.api.base.ProjectSpecificMethods;

    /**
     * Example style for wrapper usage.
     */
    public class ExamplePage extends ProjectSpecificMethods {

        /**
         * Type into a field by id.
         * @param value input text
         * @return this page
         */
        public ExamplePage typeSample(String value) {
            clearAndType(locateElement(Locators.ID, "sampleId"), value);
            reportStep("Typed sample: " + value, "pass");
            return this;
        }

        /**
         * Click a button by xpath.
         * @return this page
         */
        public ExamplePage clickSampleButton() {
            click(locateElement(Locators.XPATH, "//button[@id='sampleBtn']"));
            reportStep("Clicked sample button", "pass");
            return this;
        }
    }
    \\\`\\\`\\\`

    P — PERSONA
    Act as a senior TestLeaf framework engineer. Write clean, production-grade, enterprise code.

    O — OUTPUT FORMAT
    - Output must be a single Java class inside one \`\\\`\\\`java\` fence.
    - Include package/imports, class signature, wrapper-based interactions, JavaDocs.
    - No explanations, comments, or text outside the code fence.

    T — TONE
    Formal, concise, strict to standards.

    DOM:
    \`\`\`html
    \${domContent}
    \`\`\`
      `,

  /**
   * Cucumber Feature File Only Prompt
   */
  CUCUMBER_ONLY: `
    Instructions:
    - Generate ONLY a Cucumber (.feature) file.
    - Use Scenario Outline with Examples table.
    - Make sure every step is relevant to the provided DOM.
    - Do not combine multiple actions into one step.
    - Use South India realistic dataset (names, addresses, pin codes, mobile numbers).
    - Use dropdown values only from provided DOM.
    - Generate multiple scenarios if applicable.

    Context:
    DOM:
    \`\`\`html
    \${domContent}
    \`\`\`

    Example:
    \`\`\`gherkin
    Feature: Login to OpenTaps

    Scenario Outline: Successful login with valid credentials
      Given I open the login page
      When I type "<username>" into the Username field
      And I type "<password>" into the Password field
      And I click the Login button
      Then I should be logged in successfully

    Examples:
      | username   | password  |
      | "testuser" | "testpass"|
      | "admin"    | "admin123"|
    \`\`\`

    Persona:
    - Audience: BDD testers who only need feature files.

    Output Format:
    - Only valid Gherkin in a \`\`\`gherkin\`\`\` block.

    Tone:
    - Clear, structured, executable.
  `,

  /**
   * Cucumber with Step Definitions
   */
  CUCUMBER_WITH_SELENIUM_JAVA_STEPS: `
    Instructions:
    - Generate BOTH:
      1. A Cucumber .feature file.
      2. A Java step definition class for selenium.
    - Do NOT include Page Object code.
    - Step defs must include WebDriver setup, explicit waits, and actual Selenium code.
    - Use Scenario Outline with Examples table (South India realistic data).

    Context:
    DOM:
    \`\`\`html
    \${domContent}
    \`\`\`
    URL: \${pageUrl}

    Example:
    \`\`\`gherkin
    Feature: Login to OpenTaps

    Scenario Outline: Successful login with valid credentials
      Given I open the login page
      When I type "<username>" into the Username field
      And I type "<password>" into the Password field
      And I click the Login button
      Then I should be logged in successfully

    Examples:
      | username   | password  |
\      | "admin"    | "admin123"|
    \`\`\`

    \`\`\`java
    package com.leaftaps.stepdefs;

    import io.cucumber.java.en.*;
    import org.openqa.selenium.*;
    import org.openqa.selenium.chrome.ChromeDriver;
    import org.openqa.selenium.support.ui.*;

    public class LoginStepDefinitions {
        private WebDriver driver;
        private WebDriverWait wait;

        @io.cucumber.java.Before
        public void setUp() {
            driver = new ChromeDriver();
            wait = new WebDriverWait(driver, Duration.ofSeconds(10));
            driver.manage().window().maximize();
        }

        @io.cucumber.java.After
        public void tearDown() {
            if (driver != null) driver.quit();
        }

        @Given("I open the login page")
        public void openLoginPage() {
            driver.get("\${pageUrl}");
        }

        @When("I type {string} into the Username field")
        public void enterUsername(String username) {
            WebElement el = wait.until(ExpectedConditions.elementToBeClickable(By.id("username")));
            el.sendKeys(username);
        }

        @When("I type {string} into the Password field")
        public void enterPassword(String password) {
            WebElement el = wait.until(ExpectedConditions.elementToBeClickable(By.id("password")));
            el.sendKeys(password);
        }

        @When("I click the Login button")
        public void clickLogin() {
            driver.findElement(By.xpath("//button[contains(text(),'Login')]")).click();
        }

        @Then("I should be logged in successfully")
        public void verifyLogin() {
            WebElement success = wait.until(ExpectedConditions.visibilityOfElementLocated(By.className("success")));
            assert success.isDisplayed();
        }
    }
    \`\`\`

    Persona:
    - Audience: QA engineers working with Cucumber & Selenium.

    Output Format:
    - Gherkin in \`\`\`gherkin\`\`\` block + Java code in \`\`\`java\`\`\` block.

    Tone:
    - Professional, executable, structured.
  `
};

/**
 * Helper function to escape code blocks in prompts
 */
function escapeCodeBlocks(text) {
  return text.replace(/```/g, '\\`\\`\\`');
}

/**
 * Function to fill template variables in a prompt
 */
export function getPrompt(promptKey, variables = {}) {
  let prompt = DEFAULT_PROMPTS[promptKey];
  if (!prompt) {
    throw new Error(`Prompt not found: ${promptKey}`);
  }

  Object.entries(variables).forEach(([k, v]) => {
    const regex = new RegExp(`\\$\\{${k}\\}`, 'g');
    prompt = prompt.replace(regex, v);
  });

  return prompt.trim();
}

export const CODE_GENERATOR_TYPES = {
  SELENIUM_JAVA_PAGE_ONLY: 'Selenium-Java-Page-Only',
  CUCUMBER_ONLY: 'Cucumber-Only',
  CUCUMBER_WITH_SELENIUM_JAVA_STEPS: 'Cucumber-With-Selenium-Java-Steps',
};
