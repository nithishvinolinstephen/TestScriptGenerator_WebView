using System;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// LLM Client for OpenAI API.
    /// </summary>
    public class OpenAIClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly Core.AISettings _aiSettings;
        private readonly ILogger<OpenAIClient> _logger;

        public OpenAIClient(HttpClient httpClient, Core.AISettings aiSettings, ILogger<OpenAIClient> logger)
        {
            _httpClient = httpClient;
            _aiSettings = aiSettings;
            _logger = logger;
            
            // Set timeout once during construction to avoid "already started requests" error on retries
            _httpClient.Timeout = TimeSpan.FromSeconds(aiSettings.TimeoutSeconds);
        }

        public async Task<LLMResponse> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_aiSettings.ApiKey))
                {
                    throw new InvalidOperationException("OpenAI API key not configured");
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

                var response = await _httpClient.PostAsync(
                    $"{_aiSettings.BaseUrl}/chat/completions",
                    content,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"OpenAI API error: {response.StatusCode} - {errorContent}");
                    throw new HttpRequestException($"OpenAI API returned {response.StatusCode}");
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

                _logger.LogInformation($"OpenAI response: {totalTokens} tokens used");

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
                _logger.LogError($"OpenAI generation failed: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_aiSettings.ApiKey))
                {
                    _logger.LogWarning("OpenAI health check skipped: API key not configured");
                    return false;
                }

                var requestPayload = new
                {
                    model = _aiSettings.Model,
                    messages = new[] { new { role = "user", content = "Hello" } },
                    temperature = 0.1,
                    max_tokens = 10
                };

                var json = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_aiSettings.ApiKey}");
                _httpClient.Timeout = TimeSpan.FromSeconds(10);

                var url = $"{_aiSettings.BaseUrl}/chat/completions";
                _logger.LogInformation($"OpenAI health check: Testing connection to {url}");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("OpenAI health check passed");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"OpenAI health check failed: {response.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"OpenAI health check failed: {ex.Message}");
                return false;
            }
        }
    }
}
