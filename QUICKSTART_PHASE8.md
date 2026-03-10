# Quick Start Guide - New Prompt-Based Generation

## The New Workflow

### Step 1: Navigate to Application
```
1. Enter URL in the toolbar (e.g., https://www.example-ecommerce.com)
2. Click "Navigate" button
3. Wait for page to load
```

### Step 2: Select Elements
```
1. Click "Select Element" button to activate selection mode
2. Click on elements in the page you want to interact with:
   - Search input box
   - Search button
   - Filter dropdown
   - Add to cart button
   (as many as needed)
3. Selected elements appear in the "Elements" tab on the right panel
4. Click "Select Element" again to deactivate selection mode
```

### Step 3: Describe Your Test (NEW!)
```
1. Click the "AI Actions" tab on the right panel
2. In the text box, describe what the test should do in plain English:

Example prompts:
- "Search for apple iphone"
- "Search for apple iphone and verify results display"
- "User searches for apple iphone, clicks the first result, adds it to cart"
- "Navigate to product page, scroll to reviews, verify 5-star reviews present"

Note: DON'T describe actions for elements separately - 
just describe the overall test scenario in natural language
```

### Step 4: Choose Framework
```
1. Select from dropdown:
   - Selenium Java (JUnit)
   - Selenium C# (NUnit)
   - Playwright TypeScript (Jest)
   - Playwright .NET (NUnit)
2. Choose "AI" mode (radio button)
3. Ensure API key is configured in Settings
```

### Step 5: Generate Script
```
1. Click "Generate Script" button
2. Wait for AI to generate code (usually 5-30 seconds)
3. Review generated code in Output panel:
   - "Page Object" tab: Page Object Model class
   - "Test Class" tab: Complete test class
```

### Step 6: Copy & Use
```
1. Click "Copy Page Object" or "Copy Test Class" buttons
2. Paste code into your test project
3. Run the generated test against your application
```

## What's Different from Before?

| Before | After |
|--------|-------|
| Add actions one by one | Describe test once in prompt |
| Actions Queue panel | Single Prompt input |
| Multiple "Add Action" clicks | One prompt describes entire scenario |
| Isolated action descriptions | Natural language test description |
| Manual step ordering | AI infers sequence from prompt |

## Example Complete Flow

**Scenario:** Generate a test for searching a product on an e-commerce site

**1. Elements Selected:**
- Search input: `#search-input`
- Search button: `.btn-search`
- First result link: `.product-item:first-child a`

**2. User Prompt Entered:**
```
Search for apple iphone and click on the first result
```

**3. Generated Code Will Include:**
```java
// Page Object
public class ProductPage {
    private WebDriver driver;
    private WebDriverWait wait;
    
    private By searchInput = By.id("search-input");
    private By searchButton = By.className("btn-search");
    private By firstResult = By.cssSelector(".product-item:first-child a");
    
    public void searchForProduct(String keyword) { ... }
    public void clickFirstResult() { ... }
}

// Test Class
@Test
public void testSearchForAppleIphone() {
    driver.navigate().to("https://www.example-ecommerce.com");
    page.searchForProduct("apple iphone");
    page.clickFirstResult();
    // Assertions...
}
```

## Important Notes

✅ **DO:**
- Use natural language descriptions
- Select elements you plan to use in the test
- Enter the URL correctly before selecting elements
- Test generated code before using in production
- Make sure API key is configured in Settings

❌ **DON'T:**
- Enter action names separately (e.g., "click", "type") - describe the test flow
- Select elements you won't use
- Change framework after starting element selection
- Use special characters in prompts that might confuse the AI
- Assume generated code is production-ready without testing

## Troubleshooting

**"No elements selected" error:**
- Make sure you selected at least one element before generating

**"Please describe what the test should do" error:**
- Enter a prompt in the AI Actions tab

**Generated code doesn't match elements:**
- Verify selected elements are correct
- Re-check the prompt for clarity
- Try rephrasing the prompt

**API key required error:**
- Click Settings button
- Enter your OpenAI/Groq/Ollama API key
- Test connection
- Try generating again

## Tips for Best Results

1. **Be Specific**: Instead of "test search", use "search for apple iphone and verify results"
2. **Mention Element Types**: "Type in search box and click search button" helps AI understand
3. **Include Assertions**: "Verify results contain iphone listings" generates better test assertions
4. **Use Real Data**: "Search for apple iphone" generates better code than "search for product"
5. **One Scenario Per Test**: Keep each prompt focused on one test scenario
