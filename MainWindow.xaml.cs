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
using TestScriptGeneratorTool.Domain;
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
    private readonly ILocatorEngine _locatorEngine;
    private bool _isWebViewInitialized = false;
    private bool _isSelectionModeActive = false;
    private int _selectionCount = 0;

    public MainWindow(ILogger<MainWindow> logger, AppSettings appSettings, WebViewService webViewService, AppServices.ITestScenarioService scenarioService, ILocatorEngine locatorEngine)
    {
        _logger = logger;
        _appSettings = appSettings;
        _webViewService = webViewService;
        _scenarioService = scenarioService;
        _locatorEngine = locatorEngine;
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
            
            // Initialize step builder
            InitializeStepBuilder();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to initialize WebView2: {ex.Message}");
            StatusTextBlock.Text = $"Error: {ex.Message}";
            MessageBox.Show($"WebView2 initialization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InitializeStepBuilder()
    {
        // Populate action type dropdown
        ActionTypeComboBox.ItemsSource = Enum.GetValues(typeof(AppServices.ActionType)).Cast<AppServices.ActionType>();
        ActionTypeComboBox.SelectedIndex = 0;
        _logger.LogInformation("Step builder initialized");
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

    private async void WebViewService_ElementSelected(object? sender, ElementInfo elementInfo)
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            _selectionCount++;
            _logger.LogInformation($"Element {_selectionCount} selected: {elementInfo.Type}");

            // Create a test step
            var testStep = new AppServices.TestStep
            {
                ActionType = AppServices.ActionType.Click,
                ElementType = elementInfo.Type,
                ElementSelector = elementInfo.Selector,
                InputValue = elementInfo.Text
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
            
            // Generate and display locator
            await DisplayLocatorForElementAsync(elementInfo);
        });
    }

    private async Task DisplayLocatorForElementAsync(ElementInfo elementInfo)
    {
        try
        {
            _logger.LogInformation("Generating locator for element: {Type}", elementInfo.Type);
            
            // Create ElementDescriptor from ElementInfo
            var element = new ElementDescriptor
            {
                TagName = elementInfo.Type,
                Id = elementInfo.Id,
                ClassList = !string.IsNullOrEmpty(elementInfo.ClassName) 
                    ? elementInfo.ClassName.Split(' ').ToList() 
                    : new(),
                InnerText = elementInfo.Text,
                CssSelector = elementInfo.Selector
            };

            // Generate locator
            var locator = await _locatorEngine.GenerateLocatorAsync(element);
            _logger.LogInformation($"Locator generated - Type: {locator.LocatorType}, Locator: {locator.PrimaryLocator}");

            // Display in locator panel
            await Dispatcher.InvokeAsync(() =>
            {
                LocatorPanel.Children.Clear();

                // Primary locator
                var primaryBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(230, 245, 230)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 10),
                    CornerRadius = new CornerRadius(3)
                };

                var primaryStack = new StackPanel();
                primaryStack.Children.Add(CreateTextBlock("Primary Locator", FontWeights.Bold, 11));
                primaryStack.Children.Add(CreateTextBlock($"Type: {locator.LocatorType}", FontWeights.Normal, 10, "#666"));
                
                var primaryLocatorPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                var primaryTextBox = new TextBox
                {
                    Text = locator.PrimaryLocator,
                    IsReadOnly = false,
                    Padding = new Thickness(5),
                    Margin = new Thickness(0, 0, 5, 0),
                    Height = 30,
                    Background = new SolidColorBrush(Colors.White),
                    Foreground = new SolidColorBrush(Colors.Black),
                    FontFamily = new System.Windows.Media.FontFamily("Consolas")
                };
                primaryLocatorPanel.Children.Add(primaryTextBox);

                var editButton = new Button
                {
                    Content = "Edit",
                    Padding = new Thickness(10, 5, 10, 5),
                    Height = 30,
                    Background = new SolidColorBrush(Color.FromRgb(66, 133, 244)),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontWeight = FontWeights.Bold
                };
                editButton.Click += (s, e) => EditLocator(primaryTextBox, locator);
                primaryLocatorPanel.Children.Add(editButton);

                primaryStack.Children.Add(primaryLocatorPanel);
                primaryBorder.Child = primaryStack;
                LocatorPanel.Children.Add(primaryBorder);

                // Alternative locators
                if (locator.Alternatives.Count > 0)
                {
                    var altHeader = CreateTextBlock("Alternative Locators", FontWeights.Bold, 11);
                    altHeader.Margin = new Thickness(0, 10, 0, 5);
                    LocatorPanel.Children.Add(altHeader);

                    foreach (var alt in locator.Alternatives)
                    {
                        var altBorder = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                            BorderThickness = new Thickness(1),
                            Padding = new Thickness(10),
                            Margin = new Thickness(0, 0, 0, 5),
                            CornerRadius = new CornerRadius(2)
                        };

                        var altTextBlock = new TextBlock
                        {
                            Text = alt,
                            TextWrapping = TextWrapping.Wrap,
                            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                        };

                        altBorder.Child = altTextBlock;
                        LocatorPanel.Children.Add(altBorder);
                    }
                }

                StatusTextBlock.Text = $"Locator generated - Primary: {locator.PrimaryLocator}";
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to generate locator: {ex.Message}");
            await Dispatcher.InvokeAsync(() =>
            {
                StatusTextBlock.Text = $"Locator generation failed: {ex.Message}";
            });
        }
    }

    private void EditLocator(TextBox textBox, LocatorDefinition locator)
    {
        if (!textBox.IsReadOnly)
        {
            textBox.IsReadOnly = true;
            locator.IsUserModified = true;
            locator.PrimaryLocator = textBox.Text;
            _logger.LogInformation($"Locator updated by user: {locator.PrimaryLocator}");
            StatusTextBlock.Text = "Locator updated";
        }
        else
        {
            textBox.IsReadOnly = false;
            StatusTextBlock.Text = "Locator editing enabled";
        }
    }

    private void AddStepButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var scenario = _scenarioService.GetCurrentScenario();
            if (scenario == null)
            {
                MessageBox.Show("No scenario created. Please reload.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get values from UI
            var actionType = (AppServices.ActionType)(ActionTypeComboBox.SelectedItem ?? AppServices.ActionType.Click);
            var inputValue = InputValueTextBox.Text.Trim();

            // Validate based on action type
            if (actionType == AppServices.ActionType.Navigate && string.IsNullOrEmpty(inputValue))
            {
                MessageBox.Show("Navigation URL is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if ((actionType == AppServices.ActionType.TypeText || actionType == AppServices.ActionType.SelectDropdown) && string.IsNullOrEmpty(inputValue))
            {
                MessageBox.Show("Input value is required for this action.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create test step
            var step = new AppServices.TestStep
            {
                ActionType = actionType,
                InputValue = inputValue,
                Order = scenario.Steps.Count,
                ElementSelector = "", // Will be filled from element selection
                ElementType = ""
            };

            // Add to scenario
            _scenarioService.AddStep(step);
            _logger.LogInformation($"Step added: {step.GetDescription()}");

            // Refresh display
            RefreshStepsList();

            // Clear input
            InputValueTextBox.Text = "";
            ActionTypeComboBox.SelectedIndex = 0;

            StatusTextBlock.Text = $"Step added: {step.GetDescription()}";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to add step: {ex.Message}");
            MessageBox.Show($"Error adding step: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshStepsList()
    {
        try
        {
            StepsPanel.Children.Clear();

            var scenario = _scenarioService.GetCurrentScenario();
            if (scenario == null || scenario.Steps.Count == 0)
            {
                var noStepsText = new TextBlock
                {
                    Text = "No steps added yet",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                StepsPanel.Children.Add(noStepsText);
                return;
            }

            for (int i = 0; i < scenario.Steps.Count; i++)
            {
                var step = scenario.Steps[i];
                var stepIndex = i;

                // Create step card
                var stepBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(240, 248, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 150, 200)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 8),
                    CornerRadius = new CornerRadius(3)
                };

                var stepGrid = new Grid();
                stepGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                stepGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                stepGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                stepGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Step info
                var stepNumber = new TextBlock
                {
                    Text = $"Step {stepIndex + 1}: {step.GetDescription()}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                Grid.SetColumn(stepNumber, 0);
                Grid.SetRow(stepNumber, 0);
                stepGrid.Children.Add(stepNumber);

                // Step details
                var stepDetails = new TextBlock
                {
                    Text = $"Action: {step.ActionType} | Value: {step.InputValue ?? "(none)"}",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                };
                Grid.SetColumn(stepDetails, 0);
                Grid.SetRow(stepDetails, 1);
                stepGrid.Children.Add(stepDetails);

                // Button panel
                var buttonPanel = new StackPanel { Orientation = Orientation.Vertical };
                Grid.SetColumn(buttonPanel, 1);
                Grid.SetRowSpan(buttonPanel, 2);
                Grid.SetRow(buttonPanel, 0);

                // Move up button
                if (stepIndex > 0)
                {
                    var upButton = new Button
                    {
                        Content = "▲",
                        Width = 24,
                        Height = 24,
                        Padding = new Thickness(0),
                        FontSize = 10,
                        Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        Foreground = new SolidColorBrush(Colors.White)
                    };
                    var upIndex = stepIndex;
                    upButton.Click += (s, e) => MoveStepUp(upIndex);
                    buttonPanel.Children.Add(upButton);
                }

                // Move down button
                if (stepIndex < scenario.Steps.Count - 1)
                {
                    var downButton = new Button
                    {
                        Content = "▼",
                        Width = 24,
                        Height = 24,
                        Padding = new Thickness(0),
                        FontSize = 10,
                        Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        Foreground = new SolidColorBrush(Colors.White)
                    };
                    var downIndex = stepIndex;
                    downButton.Click += (s, e) => MoveStepDown(downIndex);
                    buttonPanel.Children.Add(downButton);
                }

                // Delete button
                var deleteButton = new Button
                {
                    Content = "✕",
                    Width = 24,
                    Height = 24,
                    Padding = new Thickness(0),
                    FontSize = 10,
                    Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    Foreground = new SolidColorBrush(Colors.White)
                };
                var deleteIndex = stepIndex;
                deleteButton.Click += (s, e) => DeleteStep(deleteIndex);
                buttonPanel.Children.Add(deleteButton);

                stepGrid.Children.Add(buttonPanel);
                stepBorder.Child = stepGrid;
                StepsPanel.Children.Add(stepBorder);
            }

            _logger.LogInformation($"Steps list refreshed: {scenario.Steps.Count} steps");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to refresh steps list: {ex.Message}");
        }
    }

    private void MoveStepUp(int index)
    {
        try
        {
            var scenario = _scenarioService.GetCurrentScenario();
            if (scenario == null || index <= 0 || index >= scenario.Steps.Count) return;

            // Swap steps
            var temp = scenario.Steps[index];
            scenario.Steps[index] = scenario.Steps[index - 1];
            scenario.Steps[index - 1] = temp;

            // Update order
            for (int i = 0; i < scenario.Steps.Count; i++)
                scenario.Steps[i].Order = i;

            RefreshStepsList();
            StatusTextBlock.Text = $"Step {index} moved up";
            _logger.LogInformation($"Step {index} moved up");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to move step up: {ex.Message}");
        }
    }

    private void MoveStepDown(int index)
    {
        try
        {
            var scenario = _scenarioService.GetCurrentScenario();
            if (scenario == null || index < 0 || index >= scenario.Steps.Count - 1) return;

            // Swap steps
            var temp = scenario.Steps[index];
            scenario.Steps[index] = scenario.Steps[index + 1];
            scenario.Steps[index + 1] = temp;

            // Update order
            for (int i = 0; i < scenario.Steps.Count; i++)
                scenario.Steps[i].Order = i;

            RefreshStepsList();
            StatusTextBlock.Text = $"Step {index + 1} moved down";
            _logger.LogInformation($"Step {index + 1} moved down");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to move step down: {ex.Message}");
        }
    }

    private void DeleteStep(int index)
    {
        try
        {
            var scenario = _scenarioService.GetCurrentScenario();
            if (scenario == null || index < 0 || index >= scenario.Steps.Count) return;

            var result = MessageBox.Show(
                $"Delete step {index + 1}: {scenario.Steps[index].GetDescription()}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            scenario.Steps.RemoveAt(index);

            // Update order
            for (int i = 0; i < scenario.Steps.Count; i++)
                scenario.Steps[i].Order = i;

            RefreshStepsList();
            StatusTextBlock.Text = $"Step {index + 1} deleted";
            _logger.LogInformation($"Step {index + 1} deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to delete step: {ex.Message}");
        }
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
