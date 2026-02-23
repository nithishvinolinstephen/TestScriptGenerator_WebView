using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestScriptGeneratorTool.Core;
using WpfApplication = System.Windows.Application;

namespace TestScriptGeneratorTool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;
    private ILogger? _logger;

    private void App_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            // Initialize Dependency Injection
            var services = new ServiceCollection();
            services.ConfigureServices();
            
            // Register core services
            services.AddSingleton<AppSettings>();
            services.AddSingleton<MainWindow>();
            
            _serviceProvider = services.BuildServiceProvider();
            _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            
            _logger.LogInformation("Application startup initiated");
            
            // Create and show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application startup failed: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("Application shutdown");
        (_serviceProvider as IDisposable)?.Dispose();
        base.OnExit(e);
    }
}

