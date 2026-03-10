# Groq API Integration - Setup Guide

## Overview
Groq API is now integrated as a third LLM provider alongside OpenAI and Ollama. Groq provides fast, cost-effective API-based inference compatible with OpenAI's chat completions format.

## Configuration

### 1. **Groq API Setup**
- Visit [Groq Console](https://console.groq.com)
- Create an API key from your account dashboard
- Keep your API key secure

### 2. **Application Settings**
When you launch the settings dialog in the application:

1. **Select Provider**: Choose "Groq" from the dropdown
2. **Base URL**: Set to `https://api.groq.com/openai/v1` (or your preferred Groq endpoint)
3. **Model**: Choose from available Groq models:
   - `mixtral-8x7b-32768` (recommended, fast)
   - `llama2-70b-4096`
   - `gemma-7b-it`
   - Other available models (check Groq console for latest)
4. **API Key**: Paste your Groq API key
5. **Temperature**: Set between 0.0 (deterministic) and 1.0 (creative) - default 0.2
6. **Max Tokens**: Maximum tokens to generate - default 4000
7. **Timeout**: Request timeout in seconds - default 30

### 3. **Test Connection**
Click the "Test Connection" button to verify your API key and settings work correctly.

## Architecture

### New Files Added
- **GroqClient.cs**: Implementation of `ILLMClient` for Groq API
  - Handles authentication via Bearer token
  - Converts requests to OpenAI-compatible format
  - Parses JSON responses from Groq API

### Modified Files
- **ServiceProvider.cs**: Updated to register GroqClient and dynamically select provider
- **SettingsWindow.xaml.cs**: Added Groq to provider dropdown and test connection logic
- **AISettings.cs**: Added documentation for Groq configuration

## API Compatibility

Groq uses OpenAI-compatible API endpoints, so the integration:
- Uses the same request/response format as OpenAI
- Supports chat completions endpoint
- Returns token usage information
- Maintains compatibility with existing code

## Comparison

| Feature | OpenAI | Groq | Ollama |
|---------|--------|------|--------|
| **Setup** | Cloud, API key required | Cloud, API key required | Local, no API key |
| **Speed** | Standard | Very Fast | Medium (hardware dependent) |
| **Cost** | Higher | Lower | Free (local) |
| **API Format** | Native | OpenAI compatible | Custom |
| **Auth** | Bearer Token | Bearer Token | None |

## Troubleshooting

### Connection Failed
- Verify API key is correct (copy from Groq console)
- Check internet connectivity
- Ensure base URL is correct: `https://api.groq.com/openai/v1`

### Invalid Model Error
- Check available models in Groq console
- Ensure model name exactly matches (case-sensitive)
- Current recommended: `mixtral-8x7b-32768`

### Rate Limiting
- Groq enforces rate limits on free tier
- Monitor your usage in Groq console
- Consider upgrading plan if needed

## Code Examples

### Using Groq in Your Tests
The provider selection is automatic based on settings:

```csharp
// Settings are configured via UI
// Application automatically selects GroqClient based on Provider setting
var coordinator = serviceProvider.GetRequiredService<IAIGenerationCoordinator>();
var result = await coordinator.GenerateTestScriptAsync(scenario);
```

## Future Enhancements
- Model selection dropdown populated from Groq API
- Streaming response support
- Cost tracking per provider
- Batch request optimization

## References
- [Groq API Documentation](https://console.groq.com/docs/api-reference)
- [Groq Models](https://console.groq.com/docs/models)
- [OpenAI Chat Completions API](https://platform.openai.com/docs/api-reference/chat)
