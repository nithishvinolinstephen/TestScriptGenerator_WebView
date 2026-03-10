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
using Microsoft.Extensions.DependencyInjection;
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
    private readonly AppServices.IScriptGenerator _scriptGenerator;
    private readonly AppServices.IAIGenerationCoordinator _aiCoordinator;
    private readonly Core.AISettings _aiSettings;
    private bool _isWebViewInitialized = false;
    private bool _isSelectionModeActive = false;
    private int _selectionCount = 0;
    private CancellationTokenSource? _generationCancellation;

    public MainWindow(ILogger<MainWindow> logger, AppSettings appSettings, WebViewService webViewService, 
        AppServices.ITestScenarioService scenarioService, ILocatorEngine locatorEngine, 
        AppServices.IScriptGenerator scriptGenerator, AppServices.IAIGenerationCoordinator aiCoordinator,
        Core.AISettings aiSettings)
    {
        _logger = logger;
        _appSettings = appSettings;
        _webViewService = webViewService;
        _scenarioService = scenarioService;
        _locatorEngine = locatorEngine;
        _scriptGenerator = scriptGenerator;
        _aiCoordinator = aiCoordinator;
        _aiSettings = aiSettings;
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
            
            // Initialize framework combobox
            InitializeFrameworkSelector();

            // Initialize UI components
            InitializeLocatorSelector();
            InitializeAIActionsPanel();
            UpdateModeUI();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to initialize WebView2: {ex.Message}");
            StatusTextBlock.Text = $"Error: {ex.Message}";
            MessageBox.Show($"WebView2 initialization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InitializeFrameworkSelector()
    {
        try
        {
            var frameworks = _scriptGenerator.GetAvailableFrameworks();
            FrameworkComboBox.ItemsSource = frameworks;
            FrameworkComboBox.SelectedIndex = 0;
            _logger.LogInformation("Framework selector initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to initialize framework selector: {ex.Message}");
        }
    }

    // Mapping of display names to technical locator type names
    private readonly Dictionary<string, string> _locatorTypeMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "XPath", "xpath" },
        { "CSS Selector", "css" },
        { "ID", "id" },
        { "Class Name", "class" },
        { "Name", "name" }
    };

    private void InitializeLocatorSelector()
    {
        try
        {
            // Populate locator type dropdown
            foreach (var displayName in _locatorTypeMapping.Keys)
            {
                LocatorTypeComboBox.Items.Add(displayName);
            }
            LocatorTypeComboBox.SelectedIndex = 0;
            _logger.LogInformation("Locator selector initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to initialize locator selector: {ex.Message}");
        }
    }

    private void InitializeAIActionsPanel()
    {
        try
        {
            // Initialize the prompt text box with placeholder text
            if (AIPromptTextBox != null)
            {
                AIPromptTextBox.Text = "";
            }
            _logger.LogInformation("AI prompt panel initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to initialize AI prompt panel: {ex.Message}");
        }
    }

    private void UpdateModeUI()
    {
        try
        {
            bool isDeterministic = DeterministicRadio.IsChecked ?? false;
            
            // Toggle tab visibility
            LocatorsTab.Visibility = isDeterministic ? Visibility.Visible : Visibility.Collapsed;
            AIActionsTab.Visibility = isDeterministic ? Visibility.Collapsed : Visibility.Visible;

            StatusTextBlock.Text = isDeterministic 
                ? "Deterministic mode - Select locator type" 
                : "AI mode - Describe your actions";

            _logger.LogInformation($"Mode updated to: {(isDeterministic ? "Deterministic" : "AI")}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to update mode UI: {ex.Message}");
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

    private async void WebViewService_ElementSelected(object? sender, ElementInfo elementInfo)
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            _selectionCount++;
            _logger.LogInformation($"Element {_selectionCount} selected: {elementInfo.Type}");

            // Generate locator first
            var locator = await GenerateLocatorForElementAsync(elementInfo);

            // Create a test step with the locator
            var testStep = new AppServices.TestStep
            {
                ActionType = AppServices.ActionType.Click,
                ElementType = elementInfo.Type,
                ElementSelector = elementInfo.Selector,
                InputValue = elementInfo.Text,
                Locator = locator
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
            
            // Display locator information
            DisplayLocatorForElementAsync(locator);
        });
    }

    private async Task<LocatorDefinition> GenerateLocatorForElementAsync(ElementInfo elementInfo)
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
            
            return locator;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to generate locator: {ex.Message}");
            // Return a basic locator using the selector
            return new LocatorDefinition
            {
                PrimaryLocator = elementInfo.Selector,
                LocatorType = "css"
            };
        }
    }

    private void DisplayLocatorForElementAsync(LocatorDefinition locator)
    {
        try
        {
            // Display in locator panel
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
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to display locator: {ex.Message}");
            StatusTextBlock.Text = $"Locator display failed: {ex.Message}";
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

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var scenario = _scenarioService.GetCurrentScenario();
            if (scenario == null || scenario.Steps.Count == 0)
            {
                MessageBox.Show("No elements selected. Please select elements first.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var useAI = AIRadio.IsChecked ?? false;

            _logger.LogInformation($"Generate button clicked. Mode selected: {(useAI ? "AI" : "DETERMINISTIC")}");

            // Validate mode-specific requirements
            if (useAI)
            {
                // For AI mode, check if a prompt is provided
                var prompt = AIPromptTextBox?.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(prompt))
                {
                    MessageBox.Show("Please describe what the test should do with the selected elements.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if elements are added (via Steps)
                if (scenario.Steps == null || scenario.Steps.Count == 0)
                {
                    MessageBox.Show("Please add at least one element to test. Use 'Add Element' button to add elements.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                // For deterministic mode, check if locator type is selected
                if (LocatorTypeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a locator type.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            StatusTextBlock.Text = $"Generating script ({(useAI ? "AI" : "Deterministic")})...";
            GenerateButton.IsEnabled = false;

            // Build generation context
            var framework = FrameworkComboBox.SelectedItem as string ?? "Selenium Java";
            var context = new AppServices.ScriptGenerationContext
            {
                Scenario = scenario,
                Framework = framework,
                PageObjectClassName = "ApplicationPage",
                TestClassName = "ApplicationTest",
                PackageName = "com.example.automation",
                Elements = BuildElementsList(scenario),
                UserPrompt = AIPromptTextBox?.Text?.Trim() ?? "",
                ApplicationUrl = UrlTextBox.Text
            };

            // Generate script
            AppServices.ScriptOutput output;
            
            if (useAI)
            {
                // Check if API key is provided
                var apiKey = await GetOrPromptForAPIKey();
                if (string.IsNullOrEmpty(apiKey))
                {
                    StatusTextBlock.Text = "API key required for AI mode";
                    GenerateButton.IsEnabled = true;
                    return;
                }

                _aiSettings.ApiKey = apiKey;
                _aiSettings.Enabled = true;

                _logger.LogInformation($"Starting AI generation with {context.Elements.Count} elements");
                _logger.LogDebug($"Elements in context: {string.Join(", ", context.Elements.Select(e => e.ElementType))}");
                _logger.LogDebug($"User prompt: {context.UserPrompt}");

                _generationCancellation = new CancellationTokenSource();
                output = await _aiCoordinator.GenerateAsync(context, _generationCancellation.Token);
                
                _logger.LogInformation($"AI generation completed. Success: {output.Success}");
                if (output.Success)
                {
                    _logger.LogInformation($"Test class code length: {output.TestClassCode?.Length ?? 0}");
                    _logger.LogInformation($"Test class first 150 chars: {output.TestClassCode?.Substring(0, Math.Min(150, output.TestClassCode?.Length ?? 0))}...");
                }
                else
                {
                    _logger.LogWarning($"AI generation failed: {output.ErrorMessage}");
                }
            }
            else
            {
                output = await _scriptGenerator.GenerateScriptAsync(context);
            }

            if (output.Success)
            {
                // Display output
                PageObjectTextBox.Text = output.PageObjectCode;
                TestClassTextBox.Text = output.TestClassCode;
                var modeUsed = useAI ? "AI" : "Deterministic";
                StatusTextBlock.Text = $"Script generated successfully ({modeUsed}) for {framework}";
                _logger.LogInformation($"Script generated ({modeUsed}): {framework}");
            }
            else
            {
                MessageBox.Show($"Generation failed: {output.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = $"Generation failed: {output.ErrorMessage}";
                _logger.LogError($"Generation failed: {output.ErrorMessage}");
            }
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Generation cancelled";
            _logger.LogInformation("Generation cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Generation error: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = "Generation error";
        }
        finally
        {
            GenerateButton.IsEnabled = true;
            _generationCancellation?.Dispose();
        }
    }

    private async Task<string?> GetOrPromptForAPIKey()
    {
        try
        {
            // Try to retrieve from credential service
            if (App.ServiceProvider == null)
                throw new InvalidOperationException("Service provider not initialized");
            
            var credService = App.ServiceProvider.GetRequiredService<Infrastructure.ICredentialService>();
            var stored = await credService.GetCredentialAsync("OpenAI.ApiKey");
            if (!string.IsNullOrEmpty(stored))
                return stored;

            // Prompt user
            var apiKeyWindow = new Window
            {
                Title = "Enter OpenAI API Key",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock { Text = "OpenAI API Key:", Margin = new Thickness(10) };
            System.Windows.Controls.Grid.SetRow(label, 0);
            grid.Children.Add(label);

            var textBox = new PasswordBox { Margin = new Thickness(10) };
            System.Windows.Controls.Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(10) };
            var okButton = new Button { Content = "OK", Width = 80, Margin = new Thickness(5, 0, 5, 0) };
            var cancelButton = new Button { Content = "Cancel", Width = 80 };

            okButton.Click += (s, e) => { apiKeyWindow.DialogResult = true; apiKeyWindow.Close(); };
            cancelButton.Click += (s, e) => { apiKeyWindow.DialogResult = false; apiKeyWindow.Close(); };

            panel.Children.Add(okButton);
            panel.Children.Add(cancelButton);
            System.Windows.Controls.Grid.SetRow(panel, 2);
            grid.Children.Add(panel);

            apiKeyWindow.Content = grid;
            
            if (apiKeyWindow.ShowDialog() == true)
            {
                var apiKey = textBox.Password;
                if (!string.IsNullOrEmpty(apiKey))
                {
                    return apiKey;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting API key: {ex.Message}");
            return null;
        }
    }

    private List<AppServices.ElementWithLocator> BuildElementsList(AppServices.TestScenario scenario)
    {
        var elements = new List<AppServices.ElementWithLocator>();
        var elementCount = 0;

        _logger.LogDebug($"Building elements list from scenario with {scenario.Steps.Count} steps");

        // In Phase 5, we create a simple element list from scenario steps
        // In future phases, we'll integrate with actual element metadata
        foreach (var step in scenario.Steps)
        {
            if (!string.IsNullOrEmpty(step.ElementSelector) && !string.IsNullOrEmpty(step.ElementType))
            {
                elementCount++;
                var elementId = $"element{elementCount}";
                var variableName = $"{step.ElementType.ToLower()}_{elementCount}";

                elements.Add(new AppServices.ElementWithLocator
                {
                    ElementId = elementId,
                    ElementType = step.ElementType,
                    Locator = step.ElementSelector,
                    LocatorType = "css",
                    VariableName = variableName
                });
                
                _logger.LogDebug($"Added element: {step.ElementType} with locator {step.ElementSelector}");
            }
        }

        _logger.LogInformation($"Built elements list with {elements.Count} elements");
        return elements;
    }

    private string FormatElementsForPrompt(AppServices.TestScenario scenario)
    {
        var sb = new StringBuilder();
        var elementCount = 0;

        foreach (var step in scenario.Steps)
        {
            if (!string.IsNullOrEmpty(step.ElementSelector) && !string.IsNullOrEmpty(step.ElementType))
            {
                elementCount++;
                sb.AppendLine($"Element {elementCount}:");
                sb.AppendLine($"  - Type: {step.ElementType}");
                sb.AppendLine($"  - Selector: {step.ElementSelector}");
                if (!string.IsNullOrEmpty(step.InputValue))
                    sb.AppendLine($"  - Text/Value: {step.InputValue}");
                sb.AppendLine();
            }
        }

        return sb.Length > 0 ? sb.ToString() : "No elements selected";
    }

    private string FormatStepsForPrompt(List<TextBlock> actionsList)
    {
        if (actionsList.Count == 0)
            return "No steps defined";

        var sb = new StringBuilder();
        var stepNumber = 1;

        foreach (var action in actionsList.OfType<TextBlock>())
        {
            if (!action.Text.Contains("No actions"))
            {
                sb.AppendLine($"Step {stepNumber}: {action.Text}");
                stepNumber++;
            }
        }

        return sb.Length > 0 ? sb.ToString() : "No steps defined";
    }

    private void CopyPageObjectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var text = PageObjectTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("No Page Object code to copy.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Clipboard.SetText(text);
            StatusTextBlock.Text = "Page Object code copied to clipboard";
            _logger.LogInformation("Page Object copied to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Copy failed: {ex.Message}");
            MessageBox.Show($"Copy failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyTestClassButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var text = TestClassTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("No Test Class code to copy.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Clipboard.SetText(text);
            StatusTextBlock.Text = "Test Class code copied to clipboard";
            _logger.LogInformation("Test Class copied to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Copy failed: {ex.Message}");
            MessageBox.Show($"Copy failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearOutputButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            PageObjectTextBox.Text = "";
            TestClassTextBox.Text = "";
            StatusTextBlock.Text = "Output cleared";
            _logger.LogInformation("Output cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Clear failed: {ex.Message}");
            MessageBox.Show($"Clear failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (App.ServiceProvider == null)
                throw new InvalidOperationException("Service provider not initialized");
            
            var credService = App.ServiceProvider.GetRequiredService<Infrastructure.ICredentialService>();
            var settingsLogger = App.ServiceProvider.GetRequiredService<ILogger<SettingsWindow>>();
            var settingsWindow = new SettingsWindow(_aiSettings, credService, settingsLogger);
            settingsWindow.Owner = this;
            
            if (settingsWindow.ShowDialog() == true)
            {
                StatusTextBlock.Text = "Settings updated successfully";
                _logger.LogInformation("AI settings updated");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Settings dialog failed: {ex.Message}");
            MessageBox.Show($"Error opening settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ModeRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        UpdateModeUI();
    }

    private void LocatorTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (LocatorTypeComboBox.SelectedItem is not string displayName)
                return;

            // Map display name to technical type name
            if (!_locatorTypeMapping.TryGetValue(displayName, out var locatorType))
                return;

            // Get currently selected elements (steps)
            var scenario = _scenarioService.GetCurrentScenario();
            if (scenario == null || scenario.Steps.Count == 0)
            {
                LocatorDisplayTextBox.Text = "No elements selected";
                LocatorCountTextBlock.Text = "";
                return;
            }

            // Display locators for all steps
            var locators = new StringBuilder();
            int count = 0;

            foreach (var step in scenario.Steps)
            {
                // Check if this step has a locator
                if (step.Locator == null)
                    continue;

                // Get the locator value for the selected type
                string? locatorValue = null;

                // Check if it's the primary locator type
                if (step.Locator.LocatorType.Equals(locatorType, StringComparison.OrdinalIgnoreCase))
                {
                    locatorValue = step.Locator.PrimaryLocator;
                }
                else if (step.Locator.TypedAlternatives.Count > 0)
                {
                    // Look for a typed alternative matching the selected type
                    var alt = step.Locator.TypedAlternatives.FirstOrDefault(a => 
                        a.LocatorType.Equals(locatorType, StringComparison.OrdinalIgnoreCase));
                    
                    if (alt != null)
                        locatorValue = alt.LocatorValue;
                }

                if (!string.IsNullOrEmpty(locatorValue))
                {
                    if (count > 0)
                        locators.AppendLine("---");
                    
                    locators.AppendLine($"Element: {step.ElementType}");
                    locators.AppendLine($"Type: {locatorType}");
                    locators.AppendLine($"Value: {locatorValue}");
                    count++;
                }
            }

            LocatorDisplayTextBox.Text = count > 0 
                ? locators.ToString() 
                : $"No {locatorType} locators found for selected elements";
            
            LocatorCountTextBlock.Text = $"Found: {count}";
            _logger.LogInformation($"Locator type changed to: {locatorType}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to update locator display: {ex.Message}");
            LocatorDisplayTextBox.Text = $"Error: {ex.Message}";
        }
    }

    private void CopyLocatorButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var locatorText = LocatorDisplayTextBox.Text;
            if (string.IsNullOrEmpty(locatorText) || locatorText.StartsWith("No "))
            {
                StatusTextBlock.Text = "No locator to copy";
                return;
            }

            Clipboard.SetText(locatorText);
            StatusTextBlock.Text = "Locator copied to clipboard!";
            _logger.LogInformation("Locator copied to clipboard");

            // Reset status after 2 seconds
            Task.Delay(2000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    StatusTextBlock.Text = "";
                });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to copy locator: {ex.Message}");
            StatusTextBlock.Text = $"Copy failed: {ex.Message}";
        }
    }

    private bool _isPanelCollapsed = false;
    private GridLength _previousMiddleColumnWidth = new GridLength(1, GridUnitType.Star);

    private void CollapsePanelButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var grid = this.FindName("Grid") as Grid;
            var middleColumn = this.FindName("MiddleColumn") as ColumnDefinition;
            
            if (middleColumn == null)
            {
                _logger.LogWarning("Could not find MiddleColumn");
                return;
            }

            if (_isPanelCollapsed)
            {
                // Expand panel
                middleColumn.Width = _previousMiddleColumnWidth;
                CollapsePanelButton.Content = ">";
                CollapsePanelButton.ToolTip = "Hide inspector panel";
                MiddlePanelBorder.Visibility = Visibility.Visible;
                _isPanelCollapsed = false;
                StatusTextBlock.Text = "Inspector expanded";
            }
            else
            {
                // Collapse panel
                _previousMiddleColumnWidth = middleColumn.Width;
                middleColumn.Width = new GridLength(0);
                CollapsePanelButton.Content = "<";
                CollapsePanelButton.ToolTip = "Show inspector panel";
                MiddlePanelBorder.Visibility = Visibility.Collapsed;
                _isPanelCollapsed = true;
                StatusTextBlock.Text = "Inspector collapsed";
            }

            _logger.LogInformation($"Inspector panel toggled - Collapsed: {_isPanelCollapsed}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to toggle panel: {ex.Message}");
        }
    }

    private void GridSplitter_MouseEnter(object sender, MouseEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.SizeWE;
    }

    private void GridSplitter_MouseLeave(object sender, MouseEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Arrow;
    }
}
