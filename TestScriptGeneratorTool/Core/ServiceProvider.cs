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
            // Configure Logging - both to console and file
            var logPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                "TestScriptGenerator");
            
            if (!System.IO.Directory.Exists(logPath))
            {
                System.IO.Directory.CreateDirectory(logPath);
            }

            var logFile = System.IO.Path.Combine(logPath, $"log_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");

            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.AddFile(logFile, minimumLevel: LogLevel.Debug);
                configure.SetMinimumLevel(LogLevel.Debug);
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
            // IMPORTANT: Use Transient (not Singleton) so that when AISettings.Provider changes,
            // the factory creates a fresh client of the new provider type
            services.AddTransient<ILLMClient>(provider =>
            {
                var settings = provider.GetRequiredService<AISettings>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                var client = settings.Provider?.ToLower() switch
                {
                    "groq" => (ILLMClient)provider.GetRequiredService<GroqClient>(),
                    "ollama" => (ILLMClient)provider.GetRequiredService<OllamaClient>(),
                    _ => (ILLMClient)provider.GetRequiredService<OpenAIClient>()
                };
                
                var logger = loggerFactory.CreateLogger<ILLMClient>();
                logger.LogDebug($"Selected LLM client: {settings.Provider} (will use: {client.GetType().Name})");
                
                return client;
            });

            services.AddSingleton<IPromptBuilder, PromptBuilder>();
            services.AddSingleton<IResponseParser, ResponseParser>();
            services.AddSingleton<ICodeValidator, CodeValidator>();
            services.AddSingleton<IAIGenerationCoordinator, AIGenerationCoordinator>();

            return services;
        }
    }
}
