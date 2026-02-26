namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// Response from LLM API.
    /// </summary>
    public class LLMResponse
    {
        /// <summary>
        /// Generated text content.
        /// </summary>
        public string Content { get; set; } = "";

        /// <summary>
        /// Total tokens used in request.
        /// </summary>
        public int TotalTokens { get; set; } = 0;

        /// <summary>
        /// Whether response is complete.
        /// </summary>
        public bool IsComplete { get; set; } = true;

        /// <summary>
        /// Finish reason (e.g., "stop", "length", "error").
        /// </summary>
        public string FinishReason { get; set; } = "stop";
    }

    /// <summary>
    /// Client for communicating with Language Learning Models.
    /// </summary>
    public interface ILLMClient
    {
        /// <summary>
        /// Send prompt to LLM and get response.
        /// </summary>
        /// <param name="prompt">The prompt to send.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>LLM response with generated content.</returns>
        Task<LLMResponse> GenerateAsync(string prompt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if LLM client is properly configured and reachable.
        /// </summary>
        /// <returns>True if client can reach the LLM service.</returns>
        Task<bool> HealthCheckAsync();
    }
}
