using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Wpf;
using TestScriptGeneratorTool.Core;
using TestScriptGeneratorTool.Infrastructure;
using AppServices = TestScriptGeneratorTool.Application;

namespace TestScriptGeneratorTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly AppSettings _appSettings;
    private readonly WebViewService _webViewService;
    private readonly AppServices.ITestScenarioService _scenarioService;
    private bool _isWebViewInitialized = false;
    private bool _isSelectionModeActive = false;
    private int _selectionCount = 0;

    public MainWindow(ILogger<MainWindow> logger, AppSettings appSettings, WebViewService webViewService, AppServices.ITestScenarioService scenarioService)
    {
        _logger = logger;
        _appSettings = appSettings;
        _webViewService = webViewService;
        _scenarioService = scenarioService;
        InitializeComponent();
        _logger.LogInformation($"MainWindow initialized - App: {_appSettings.AppName} v{_appSettings.AppVersion}");
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("MainWindow loaded, initializing WebView2");
            StatusTextBlock.Text = "Initializing WebView2...";
            
            await _webViewService.InitializeWebViewAsync(WebView);
            _isWebViewInitialized = true;
            
            // Set default URL
            UrlTextBox.Text = "https://www.example.com";
            _webViewService.Navigate(UrlTextBox.Text);
            
            StatusTextBlock.Text = "Ready";
            _logger.LogInformation("WebView2 initialized successfully");
            
            // Subscribe to events
            _webViewService.NavigationCompleted += (s, url) =>
            {
                StatusTextBlock.Text = $"Loaded: {url}";
            };
            
            _webViewService.NavigationFailed += (s, error) =>
            {
                StatusTextBlock.Text = $"Error: {error}";
            };

            _webViewService.ElementSelected += WebViewService_ElementSelected;

            // Create default scenario
            _scenarioService.CreateScenario("Default Scenario", "Test scenario for element selection");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to initialize WebView2: {ex.Message}");
            StatusTextBlock.Text = $"Error: {ex.Message}";
            MessageBox.Show($"WebView2 initialization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NavigateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isWebViewInitialized)
        {
            MessageBox.Show("WebView2 is not initialized yet.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var url = UrlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Please enter a URL", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Add scheme if not present
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "https://" + url;
            UrlTextBox.Text = url;
        }

        StatusTextBlock.Text = "Navigating...";
        _webViewService.Navigate(url);
        _logger.LogInformation($"Navigation requested to: {url}");
    }

    private async void SelectElementButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isWebViewInitialized)
        {
            MessageBox.Show("WebView2 is not initialized yet.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _isSelectionModeActive = !_isSelectionModeActive;
        _logger.LogInformation($"Selection mode toggle: {_isSelectionModeActive}");

        if (_isSelectionModeActive)
        {
            SelectElementButton.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            SelectElementButton.Foreground = new SolidColorBrush(Colors.White);
            SelectElementButton.Content = "Stop Selection";
            StatusTextBlock.Text = "Selection mode ACTIVE - Click on elements to select them";
            _logger.LogInformation("Element selection mode enabled");
            await _webViewService.EnableElementSelectionAsync();
        }
        else
        {
            SelectElementButton.Background = SystemColors.ControlBrush;
            SelectElementButton.Foreground = SystemColors.ControlTextBrush;
            SelectElementButton.Content = "Select Element";
            StatusTextBlock.Text = "Selection mode DISABLED - Ready";
            _logger.LogInformation("Element selection mode disabled");
            await _webViewService.DisableElementSelectionAsync();
        }
    }

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        SelectionPanel.Children.Clear();
        _selectionCount = 0;
        StatusTextBlock.Text = "Selection cleared";
        _logger.LogInformation("Selection cleared");
    }

    private void WebViewService_ElementSelected(object? sender, ElementInfo elementInfo)
    {
        Dispatcher.Invoke(() =>
        {
            _selectionCount++;
            _logger.LogInformation($"Element {_selectionCount} selected: {elementInfo.Type}");

            // Create a test step
            var testStep = new AppServices.TestStep
            {
                Action = "click",
                ElementType = elementInfo.Type,
                ElementSelector = elementInfo.Selector,
                Value = elementInfo.Text
            };

            // Add to scenario
            var scenario = _scenarioService.GetCurrentScenario();
            if (scenario != null)
            {
                _scenarioService.AddStep(testStep);
                _logger.LogInformation($"Test step added to scenario: {scenario.Name}");
            }

            // Display in selection panel
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                CornerRadius = new CornerRadius(3)
            };

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(CreateTextBlock($"#{_selectionCount} {elementInfo.Type.ToUpper()}", FontWeights.Bold, 12));
            stackPanel.Children.Add(CreateTextBlock($"Selector: {elementInfo.Selector}", FontWeights.Normal, 10, "#666"));
            
            if (!string.IsNullOrEmpty(elementInfo.Text))
                stackPanel.Children.Add(CreateTextBlock($"Text: {elementInfo.Text}", FontWeights.Normal, 10, "#666"));
            
            if (!string.IsNullOrEmpty(elementInfo.Id))
                stackPanel.Children.Add(CreateTextBlock($"ID: {elementInfo.Id}", FontWeights.Normal, 10, "#666"));
            
            if (!string.IsNullOrEmpty(elementInfo.ClassName))
                stackPanel.Children.Add(CreateTextBlock($"Class: {elementInfo.ClassName}", FontWeights.Normal, 10, "#666"));

            border.Child = stackPanel;
            SelectionPanel.Children.Add(border);

            StatusTextBlock.Text = $"Element {_selectionCount} selected: {elementInfo.Type}";
        });
    }

    private TextBlock CreateTextBlock(string text, FontWeight weight, int size, string color = "#000")
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontWeight = weight,
            FontSize = size,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 3)
        };

        // Parse color
        if (color.StartsWith("#"))
        {
            var brush = new BrushConverter().ConvertFromString(color) as SolidColorBrush ?? SystemColors.ControlTextBrush;
            textBlock.Foreground = brush;
        }

        return textBlock;
    }
}
