namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// Context data for script generation templates.
    /// </summary>
    public class ScriptGenerationContext
    {
        /// <summary>
        /// The test scenario containing all steps.
        /// </summary>
        public TestScenario? Scenario { get; set; }

        /// <summary>
        /// Target framework (e.g., "Selenium Java", "Playwright TypeScript").
        /// </summary>
        public string Framework { get; set; } = "Selenium Java";

        /// <summary>
        /// Generated page object class name.
        /// </summary>
        public string PageObjectClassName { get; set; } = "ApplicationPage";

        /// <summary>
        /// Generated test class name.
        /// </summary>
        public string TestClassName { get; set; } = "ApplicationTest";

        /// <summary>
        /// Package/namespace name.
        /// </summary>
        public string PackageName { get; set; } = "com.example.automation";

        /// <summary>
        /// List of all selected elements with their locators.
        /// </summary>
        public List<ElementWithLocator> Elements { get; set; } = new();
    }

    /// <summary>
    /// Represents an element and its locator for template rendering.
    /// </summary>
    public class ElementWithLocator
    {
        /// <summary>
        /// Element identifier for variable naming.
        /// </summary>
        public string ElementId { get; set; } = "";

        /// <summary>
        /// Element type (button, input, etc.).
        /// </summary>
        public string ElementType { get; set; } = "";

        /// <summary>
        /// Primary locator string.
        /// </summary>
        public string Locator { get; set; } = "";

        /// <summary>
        /// Locator type (id, css, xpath, etc.).
        /// </summary>
        public string LocatorType { get; set; } = "css";

        /// <summary>
        /// Variable name for this element (e.g., "submitButton").
        /// </summary>
        public string VariableName { get; set; } = "";
    }
}
