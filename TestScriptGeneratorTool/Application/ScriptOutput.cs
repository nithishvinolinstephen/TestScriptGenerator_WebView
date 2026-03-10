namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// Represents generated test script output.
    /// </summary>
    public class ScriptOutput
    {
        /// <summary>
        /// The framework/language for this output (e.g., "Selenium Java", "Playwright TypeScript").
        /// </summary>
        public string Framework { get; set; } = "Selenium Java";

        /// <summary>
        /// Page Object class code (if applicable to framework).
        /// </summary>
        public string PageObjectCode { get; set; } = "";

        /// <summary>
        /// Test class code.
        /// </summary>
        public string TestClassCode { get; set; } = "";

        /// <summary>
        /// Whether generation was successful.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Error message if generation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp when script was generated.
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Get the combined code output (Page Object + Test Class).
        /// </summary>
        public string GetCombinedOutput()
        {
            var output = "";
            if (!string.IsNullOrEmpty(PageObjectCode))
                output += PageObjectCode + "\n\n";
            output += TestClassCode;
            return output;
        }
    }
}
