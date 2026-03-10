using TestScriptGeneratorTool.Domain;

namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// Enumeration of all supported test actions.
    /// </summary>
    public enum ActionType
    {
        Click,
        TypeText,
        SelectDropdown,
        Hover,
        Navigate,
        AssertVisible,
        AssertTextEquals,
        AssertAttribute,
        WaitForElement,
        UploadFile,
        Screenshot
    }

    /// <summary>
    /// Assertion rule for verification steps.
    /// </summary>
    public class AssertionRule
    {
        public string AssertionType { get; set; } = ""; // "visible", "text", "attribute"
        public string ExpectedValue { get; set; } = "";
        public string? AttributeName { get; set; }
    }

    /// <summary>
    /// Represents a test scenario/script.
    /// </summary>
    public class TestScenario
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<TestStep> Steps { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Represents a single test step.
    /// </summary>
    public class TestStep
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ActionType ActionType { get; set; } = ActionType.Click;
        public string ElementSelector { get; set; } = "";
        public string ElementType { get; set; } = "";
        public LocatorDefinition? Locator { get; set; } // Full locator with alternatives
        public string? InputValue { get; set; } = ""; // For TypeText, SelectDropdown actions
        public AssertionRule? Assertion { get; set; } // For assertion actions
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets a user-friendly description of this step.
        /// </summary>
        public string GetDescription()
        {
            return ActionType switch
            {
                ActionType.Click => $"Click {ElementType}",
                ActionType.TypeText => $"Type '{InputValue}' in {ElementType}",
                ActionType.SelectDropdown => $"Select '{InputValue}' in {ElementType}",
                ActionType.Hover => $"Hover over {ElementType}",
                ActionType.Navigate => $"Navigate to {InputValue}",
                ActionType.AssertVisible => $"Assert {ElementType} is visible",
                ActionType.AssertTextEquals => $"Assert {ElementType} text equals '{Assertion?.ExpectedValue}'",
                ActionType.AssertAttribute => $"Assert {ElementType} {Assertion?.AttributeName} equals '{Assertion?.ExpectedValue}'",
                ActionType.WaitForElement => $"Wait for {ElementType}",
                ActionType.UploadFile => $"Upload file '{InputValue}' to {ElementType}",
                ActionType.Screenshot => "Take screenshot",
                _ => "Unknown action"
            };
        }
    }
}
