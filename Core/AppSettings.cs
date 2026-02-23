namespace TestScriptGeneratorTool.Core
{
    /// <summary>
    /// Application-wide configuration settings.
    /// </summary>
    public class AppSettings
    {
        public string AppName { get; set; } = "Test Automation Script Generator";
        public string AppVersion { get; set; } = "0.1.0-phase0";
        public string DefaultBrowserUrl { get; set; } = "about:blank";
        public int BrowserLoadTimeoutMs { get; set; } = 10000;
    }
}
