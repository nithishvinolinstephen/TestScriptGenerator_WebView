using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

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
        private readonly IServiceProvider _serviceProvider;
        private readonly IPromptBuilder _promptBuilder;
        private readonly IResponseParser _responseParser;
        private readonly ICodeValidator _codeValidator;
        private readonly IScriptGenerator _deterministic;
        private readonly Core.AISettings _aiSettings;
        private readonly ILogger<AIGenerationCoordinator> _logger;

        public AIGenerationCoordinator(
            IServiceProvider serviceProvider,
            IPromptBuilder promptBuilder,
            IResponseParser responseParser,
            ICodeValidator codeValidator,
            IScriptGenerator deterministic,
            Core.AISettings aiSettings,
            ILogger<AIGenerationCoordinator> logger)
        {
            _serviceProvider = serviceProvider;
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
                _logger.LogInformation($"AIGenerationCoordinator.GenerateAsync called with context: Framework={context.Framework}, Elements={context.Elements.Count}, UserPrompt length={context.UserPrompt?.Length ?? 0}");
                
                if (!_aiSettings.Enabled)
                {
                    _logger.LogInformation("AI generation disabled, using deterministic mode");
                    return await _deterministic.GenerateScriptAsync(context);
                }

                // Get a fresh ILLMClient instance that respects the current provider setting
                // This must be done dynamically on each call, not during construction
                var llmClient = _serviceProvider.GetRequiredService<ILLMClient>();
                _logger.LogInformation($"Got LLM client for provider: {_aiSettings.Provider}");

                // Skip health check - attempt AI generation directly and fall back on actual failure
                // Health checks can fail due to network issues but actual API calls may succeed
                
                // Retry loop
                var failures = new List<string>();
                ScriptOutput? bestAttempt = null;
                
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

                        _logger.LogDebug($"Built prompt of {prompt.Length} characters");

                        // Call LLM
                        var llmResponse = await llmClient.GenerateAsync(prompt, cancellationToken);
                        _logger.LogInformation($"LLM returned response with {llmResponse.TotalTokens} tokens, content length={llmResponse.Content.Length}");
                        _logger.LogDebug($"LLM response preview: {llmResponse.Content.Substring(0, Math.Min(200, llmResponse.Content.Length))}...");

                        // Parse response
                        var parsedOutput = _responseParser.ParseResponse(
                            llmResponse.Content,
                            context.PageObjectClassName,
                            context.TestClassName);

                        // Keep track of the best attempt even if validation fails
                        if (parsedOutput.Success && bestAttempt == null)
                        {
                            bestAttempt = parsedOutput;
                            _logger.LogDebug("Successfully parsed AI response");
                        }

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

                // If we got a parsed response (even if validation failed), return it
                // This ensures the user sees the AI response, not the template fallback
                if (bestAttempt != null)
                {
                    _logger.LogInformation("Returning best parsed AI response (validation may have issues)");
                    return bestAttempt;
                }

                // Only fall back to deterministic if we completely failed to get any AI response
                _logger.LogWarning($"All AI retries exhausted with no parsed response, falling back to deterministic generation");
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
