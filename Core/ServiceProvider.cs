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

            // Register Infrastructure Services
            services.AddSingleton<ISelectionService, SelectionService>();
            services.AddSingleton<WebViewService>();
            services.AddSingleton<ILocatorEngine, LocatorEngine>();

            // Register Application Services
            services.AddSingleton<ITestScenarioService, TestScenarioService>();

            return services;
        }
    }
}
