namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// Service for generating test scripts from scenarios.
    /// </summary>
    public interface IScriptGenerator
    {
        /// <summary>
        /// Generate test script from scenario using template.
        /// </summary>
        /// <param name="context">Script generation context with scenario and settings.</param>
        /// <returns>Generated script output.</returns>
        Task<ScriptOutput> GenerateScriptAsync(ScriptGenerationContext context);

        /// <summary>
        /// Get available framework templates.
        /// </summary>
        /// <returns>List of supported framework names.</returns>
        List<string> GetAvailableFrameworks();
    }
}
