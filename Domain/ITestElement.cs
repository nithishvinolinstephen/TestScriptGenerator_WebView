namespace TestScriptGeneratorTool.Domain
{
    /// <summary>
    /// Represents a test element (UI control) on a web page.
    /// </summary>
    public interface ITestElement
    {
        string Id { get; }
        string ElementType { get; }
        string Selector { get; }
        string Text { get; }
        Dictionary<string, string> Attributes { get; }
    }

    /// <summary>
    /// Implementation of ITestElement.
    /// </summary>
    public class TestElement : ITestElement
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ElementType { get; set; } = "";
        public string Selector { get; set; } = "";
        public string Text { get; set; } = "";
        public Dictionary<string, string> Attributes { get; set; } = new();
    }
}
