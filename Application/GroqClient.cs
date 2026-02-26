using System;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// LLM Client for Groq API.
    /// Groq provides OpenAI-compatible API endpoints for fast inference.
    /// </summary>
    public class GroqClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly Core.AISettings _aiSettings;
        private readonly ILogger<GroqClient> _logger;

        public GroqClient(HttpClient httpClient, Core.AISettings aiSettings, ILogger<GroqClient> logger)
        {
            _httpClient = httpClient;
            _aiSettings = aiSettings;
            _logger = logger;
        }

        public async Task<LLMResponse> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_aiSettings.ApiKey))
                {
                    throw new InvalidOperationException("Groq API key not configured");
                }

                var requestPayload = new
                {
                    model = _aiSettings.Model,
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature = _aiSettings.Temperature,
                    max_tokens = _aiSettings.MaxTokens
                };

                var json = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_aiSettings.ApiKey}");
                _httpClient.Timeout = TimeSpan.FromSeconds(_aiSettings.TimeoutSeconds);

                var response = await _httpClient.PostAsync(
                    $"{_aiSettings.BaseUrl}/chat/completions",
                    content,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Groq API error: {response.StatusCode} - {errorContent}");
                    throw new HttpRequestException($"Groq API returned {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent, options);

                var choices = jsonResponse.GetProperty("choices");
                var firstChoice = choices[0];
                var message = firstChoice.GetProperty("message");
                var generatedText = message.GetProperty("content").GetString() ?? "";

                var usage = jsonResponse.GetProperty("usage");
                var totalTokens = usage.GetProperty("total_tokens").GetInt32();

                _logger.LogInformation($"Groq response: {totalTokens} tokens used");

                return new LLMResponse
                {
                    Content = generatedText,
                    TotalTokens = totalTokens,
                    IsComplete = true,
                    FinishReason = firstChoice.GetProperty("finish_reason").GetString() ?? "stop"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Groq generation failed: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_aiSettings.ApiKey))
                    return false;

                if (string.IsNullOrEmpty(_aiSettings.Model))
                    throw new InvalidOperationException("Model is required for health check");

                var requestPayload = new
                {
                    model = _aiSettings.Model,
                    messages = new[] { new { role = "user", content = "test" } },
                    max_tokens = 5
                };

                var json = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_aiSettings.ApiKey}");
                _httpClient.Timeout = TimeSpan.FromSeconds(10);

                var fullUrl = $"{_aiSettings.BaseUrl}/chat/completions";
                _logger.LogInformation($"Groq health check URL: {fullUrl}");

                var response = await _httpClient.PostAsync(fullUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Groq health check failed with status {response.StatusCode}: {errorContent}");
                    throw new HttpRequestException($"Groq API returned {response.StatusCode}: {errorContent}");
                }

                _logger.LogInformation("Groq health check successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Groq health check failed: {ex}");
                throw;
            }
        }
    }
}
