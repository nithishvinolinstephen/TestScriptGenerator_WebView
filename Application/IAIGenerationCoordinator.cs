using Microsoft.Extensions.Logging;

namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// Coordinator for AI-based script generation with validation and retry logic.
    /// </summary>
    public interface IAIGenerationCoordinator
    {
        /// <summary>
        /// Generate script using AI with automatic retry and fallback logic.
        /// </summary>
        /// <param name="context">Script generation context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Generated script output.</returns>
        Task<ScriptOutput> GenerateAsync(ScriptGenerationContext context, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of IAIGenerationCoordinator.
    /// </summary>
    public class AIGenerationCoordinator : IAIGenerationCoordinator
    {
        private readonly ILLMClient _llmClient;
        private readonly IPromptBuilder _promptBuilder;
        private readonly IResponseParser _responseParser;
        private readonly ICodeValidator _codeValidator;
        private readonly IScriptGenerator _deterministic;
        private readonly Core.AISettings _aiSettings;
        private readonly ILogger<AIGenerationCoordinator> _logger;

        public AIGenerationCoordinator(
            ILLMClient llmClient,
            IPromptBuilder promptBuilder,
            IResponseParser responseParser,
            ICodeValidator codeValidator,
            IScriptGenerator deterministic,
            Core.AISettings aiSettings,
            ILogger<AIGenerationCoordinator> logger)
        {
            _llmClient = llmClient;
            _promptBuilder = promptBuilder;
            _responseParser = responseParser;
            _codeValidator = codeValidator;
            _deterministic = deterministic;
            _aiSettings = aiSettings;
            _logger = logger;
        }

        public async Task<ScriptOutput> GenerateAsync(ScriptGenerationContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_aiSettings.Enabled)
                {
                    _logger.LogInformation("AI generation disabled, using deterministic mode");
                    return await _deterministic.GenerateScriptAsync(context);
                }

                // Check LLM health
                var isHealthy = await _llmClient.HealthCheckAsync();
                if (!isHealthy)
                {
                    _logger.LogWarning("LLM health check failed, falling back to deterministic");
                    return await _deterministic.GenerateScriptAsync(context);
                }

                // Retry loop
                var failures = new List<string>();
                for (int attempt = 1; attempt <= _aiSettings.MaxRetries; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        _logger.LogInformation($"AI generation attempt {attempt} of {_aiSettings.MaxRetries}");

                        // Build prompt
                        var prompt = attempt == 1
                            ? _promptBuilder.BuildGenerationPrompt(context)
                            : _promptBuilder.BuildRepairPrompt(context, failures);

                        // Call LLM
                        var llmResponse = await _llmClient.GenerateAsync(prompt, cancellationToken);

                        // Parse response
                        var parsedOutput = _responseParser.ParseResponse(
                            llmResponse.Content,
                            context.PageObjectClassName,
                            context.TestClassName);

                        if (!parsedOutput.Success)
                        {
                            _logger.LogWarning($"Response parsing failed: {parsedOutput.ErrorMessage}");
                            failures.Add(parsedOutput.ErrorMessage ?? "Parsing failed");
                            continue;
                        }

                        // Validate code
                        var validation = _codeValidator.ValidateCode(
                            parsedOutput.PageObjectCode,
                            parsedOutput.TestClassCode,
                            context.PageObjectClassName,
                            context.TestClassName);

                        if (validation.IsValid)
                        {
                            _logger.LogInformation($"Code validation passed on attempt {attempt}");
                            return parsedOutput;
                        }

                        // Store failures for repair prompt
                        failures = validation.Failures;
                        _logger.LogWarning($"Code validation failed: {string.Join(", ", validation.Failures)}");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("AI generation cancelled by user");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Generation attempt {attempt} failed: {ex.Message}");
                        failures.Add($"Attempt {attempt} failed: {ex.Message}");
                    }
                }

                // All retries exhausted, fall back to deterministic
                _logger.LogWarning($"All AI retries exhausted, falling back to deterministic generation");
                return await _deterministic.GenerateScriptAsync(context);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Generation cancelled");
                return new ScriptOutput
                {
                    Success = false,
                    ErrorMessage = "Generation cancelled by user"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Generation coordinator error: {ex.Message}");
                return new ScriptOutput
                {
                    Success = false,
                    ErrorMessage = $"Generation error: {ex.Message}"
                };
            }
        }
    }
}
