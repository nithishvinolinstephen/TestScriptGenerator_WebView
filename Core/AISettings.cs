namespace TestScriptGeneratorTool.Core
{
    /// <summary>
    /// AI/LLM configuration settings.
    /// Supports multiple providers: OpenAI, Groq, and Ollama.
    /// - OpenAI: https://api.openai.com/v1 (requires API key)
    /// - Groq: https://api.groq.com/openai/v1 (requires API key, fast inference)
    /// - Ollama: http://localhost:11434 (local, no API key required)
    /// </summary>
    public class AISettings
    {
        /// <summary>
        /// Base URL for LLM service.
        /// Examples:
        /// - OpenAI: https://api.openai.com/v1
        /// - Groq: https://api.groq.com/openai/v1
        /// - Ollama: http://localhost:11434
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";

        /// <summary>
        /// LLM provider type: "OpenAI", "Groq", or "Ollama".
        /// </summary>
        public string Provider { get; set; } = "OpenAI";

        /// <summary>
        /// Model name to use (e.g., "gpt-4", "llama2").
        /// </summary>
        public string Model { get; set; } = "gpt-4";

        /// <summary>
        /// API key for authentication (stored securely, not in config).
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Request timeout in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum retries on failure.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Temperature parameter for LLM (0.0 = deterministic, 1.0 = creative).
        /// </summary>
        public double Temperature { get; set; } = 0.2;

        /// <summary>
        /// Maximum tokens to generate.
        /// </summary>
        public int MaxTokens { get; set; } = 4000;

        /// <summary>
        /// Whether to enable AI mode (vs deterministic only).
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
