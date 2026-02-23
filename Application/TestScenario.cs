namespace TestScriptGeneratorTool.Application
{
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
        public string Action { get; set; } = ""; // click, input, verify, navigate
        public string ElementSelector { get; set; } = "";
        public string ElementType { get; set; } = "";
        public string Value { get; set; } = ""; // For input actions
        public string ExpectedResult { get; set; } = "";
        public int Order { get; set; }
    }
}
