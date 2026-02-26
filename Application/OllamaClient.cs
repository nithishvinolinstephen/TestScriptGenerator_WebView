using System;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// LLM Client for Ollama local models.
    /// </summary>
    public class OllamaClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly Core.AISettings _aiSettings;
        private readonly ILogger<OllamaClient> _logger;

        public OllamaClient(HttpClient httpClient, Core.AISettings aiSettings, ILogger<OllamaClient> logger)
        {
            _httpClient = httpClient;
            _aiSettings = aiSettings;
            _logger = logger;
        }

        public async Task<LLMResponse> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestPayload = new
                {
                    model = _aiSettings.Model,
                    prompt = prompt,
                    temperature = _aiSettings.Temperature,
                    num_predict = _aiSettings.MaxTokens,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                _httpClient.Timeout = TimeSpan.FromSeconds(_aiSettings.TimeoutSeconds);

                var response = await _httpClient.PostAsync(
                    $"{_aiSettings.BaseUrl}/api/generate",
                    content,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Ollama API error: {response.StatusCode} - {errorContent}");
                    throw new HttpRequestException($"Ollama API returned {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent, options);

                var generatedText = jsonResponse.GetProperty("response").GetString() ?? "";

                _logger.LogInformation($"Ollama response: Generated {generatedText.Length} characters");

                return new LLMResponse
                {
                    Content = generatedText,
                    TotalTokens = 0, // Ollama doesn't return token count
                    IsComplete = true,
                    FinishReason = "stop"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ollama generation failed: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(5);
                var response = await _httpClient.GetAsync($"{_aiSettings.BaseUrl}/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Ollama health check failed: {ex.Message}");
                return false;
            }
        }
    }
}
