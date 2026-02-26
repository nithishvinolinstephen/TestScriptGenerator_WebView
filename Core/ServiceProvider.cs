using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestScriptGeneratorTool.Application;
using TestScriptGeneratorTool.Domain;
using TestScriptGeneratorTool.Infrastructure;

namespace TestScriptGeneratorTool.Core
{
    /// <summary>
    /// Configures and provides dependency injection container for the application.
    /// </summary>
    public static class ServiceConfiguration
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            // Configure Logging
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Information);
            });

            // Configure AI Settings
            var aiSettings = new AISettings
            {
                BaseUrl = "https://api.openai.com/v1",
                Provider = "OpenAI",
                Model = "gpt-4",
                Enabled = false // Default disabled, user enables via UI
            };
            services.AddSingleton(aiSettings);

            // Register Infrastructure Services
            services.AddSingleton<ISelectionService, SelectionService>();
            services.AddSingleton<WebViewService>();
            services.AddSingleton<ILocatorEngine, LocatorEngine>();
            services.AddSingleton<ICredentialService, WindowsCredentialService>();

            // Register Application Services
            services.AddSingleton<ITestScenarioService, TestScenarioService>();
            services.AddSingleton<IScriptGenerator, ScriptGenerationService>();

            // Register Phase 6 AI Services
            services.AddHttpClient<OpenAIClient>();
            services.AddHttpClient<GroqClient>();
            services.AddHttpClient<OllamaClient>();
            
            // Register ILLMClient factory to select provider dynamically
            services.AddSingleton<ILLMClient>(provider =>
            {
                var settings = provider.GetRequiredService<AISettings>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                return settings.Provider?.ToLower() switch
                {
                    "groq" => provider.GetRequiredService<GroqClient>(),
                    "ollama" => provider.GetRequiredService<OllamaClient>(),
                    _ => provider.GetRequiredService<OpenAIClient>()
                };
            });

            services.AddSingleton<IPromptBuilder, PromptBuilder>();
            services.AddSingleton<IResponseParser, ResponseParser>();
            services.AddSingleton<ICodeValidator, CodeValidator>();
            services.AddSingleton<IAIGenerationCoordinator, AIGenerationCoordinator>();

            return services;
        }
    }
}
