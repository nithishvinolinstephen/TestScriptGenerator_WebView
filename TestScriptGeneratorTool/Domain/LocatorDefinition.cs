namespace TestScriptGeneratorTool.Domain
{
    /// <summary>
    /// Represents a single alternative locator with type information.
    /// </summary>
    public class AlternativeLocator
    {
        public string LocatorType { get; set; } = ""; // "id", "name", "css", "xpath", etc.
        public string LocatorValue { get; set; } = "";
    }

    /// <summary>
    /// Defines a locator for an element with primary and alternative locators.
    /// </summary>
    public class LocatorDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PrimaryLocator { get; set; } = "";
        public string LocatorType { get; set; } = ""; // "id", "name", "css", "xpath", "data-qa"
        public List<string> Alternatives { get; set; } = new();
        public List<AlternativeLocator> TypedAlternatives { get; set; } = new();
        public bool IsUserModified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Element descriptor with all metadata for locator generation.
    /// </summary>
    public class ElementDescriptor
    {
        public string TagName { get; set; } = "";
        public string? Id { get; set; }
        public string? Name { get; set; }
        public List<string> ClassList { get; set; } = new();
        public Dictionary<string, string> Attributes { get; set; } = new();
        public string InnerText { get; set; } = "";
        public string CssSelector { get; set; } = "";
        public string XPath { get; set; } = "";
        public BoundingRect BoundingRect { get; set; } = new();
        public List<int> FramePath { get; set; } = new();
        public List<string> ShadowHostChain { get; set; } = new();
    }

    /// <summary>
    /// Bounding rectangle of an element.
    /// </summary>
    public class BoundingRect
    {
        public int Top { get; set; }
        public int Left { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
