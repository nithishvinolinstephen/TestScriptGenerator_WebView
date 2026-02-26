using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace TestScriptGeneratorTool
{
    /// <summary>
    /// Settings window for AI configuration.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly Core.AISettings _aiSettings;
        private readonly Infrastructure.ICredentialService _credentialService;
        private readonly ILogger<SettingsWindow> _logger;

        public SettingsWindow(Core.AISettings aiSettings, Infrastructure.ICredentialService credentialService, ILogger<SettingsWindow> logger)
        {
            _aiSettings = aiSettings;
            _credentialService = credentialService;
            _logger = logger;
            InitializeComponent();
            InitializeProviderDropdown();
            TemperatureSlider.ValueChanged += TemperatureSlider_ValueChanged;
            LoadSettings();
        }

        private void InitializeProviderDropdown()
        {
            ProviderComboBox.Items.Add("OpenAI");
            ProviderComboBox.Items.Add("Groq");
            ProviderComboBox.Items.Add("Ollama");
            ProviderComboBox.SelectedIndex = 0;
        }

        private void TemperatureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TemperatureValueBlock.Text = e.NewValue.ToString("F2");
        }

        private async void LoadSettings()
        {
            try
            {
                // Set provider
                ProviderComboBox.SelectedItem = _aiSettings.Provider ?? "OpenAI";

                // Load model
                ModelTextBox.Text = _aiSettings.Model ?? "gpt-4";

                // Load base URL
                BaseUrlTextBox.Text = _aiSettings.BaseUrl ?? "https://api.openai.com/v1";

                // Load API key for the currently selected provider
                var provider = _aiSettings.Provider ?? "OpenAI";
                var credentialKey = $"{provider}.ApiKey";
                
                if (await _credentialService.CredentialExistsAsync(credentialKey))
                {
                    var apiKey = await _credentialService.GetCredentialAsync(credentialKey);
                    ApiKeyPasswordBox.Password = apiKey ?? "";
                }

                // Load temperature
                TemperatureSlider.Value = _aiSettings.Temperature;

                // Load max retries
                MaxRetriesTextBox.Text = _aiSettings.MaxRetries.ToString();

                // Load max tokens
                MaxTokensTextBox.Text = _aiSettings.MaxTokens.ToString();

                StatusTextBlock.Text = "Settings loaded successfully.";
                StatusTextBlock.Foreground = Brushes.Green;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load settings: {ex.Message}");
                StatusTextBlock.Text = $"Error loading settings: {ex.Message}";
                StatusTextBlock.Foreground = Brushes.Red;
            }
        }

        private string GetDefaultBaseUrl(string provider)
        {
            return provider?.ToLower() switch
            {
                "groq" => "https://api.groq.com/openai/v1",
                "ollama" => "http://localhost:11434",
                "openai" => "https://api.openai.com/v1",
                _ => "https://api.openai.com/v1"
            };
        }

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Update base URL when provider changes
                var selectedProvider = ProviderComboBox.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedProvider) && BaseUrlTextBox.Text == GetDefaultBaseUrl(_aiSettings.Provider))
                {
                    // Only update if the current URL matches the previous default
                    BaseUrlTextBox.Text = GetDefaultBaseUrl(selectedProvider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update base URL: {ex.Message}");
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (!int.TryParse(MaxRetriesTextBox.Text, out var maxRetries) || maxRetries < 1)
                {
                    MessageBox.Show("Max Retries must be a positive integer", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(MaxTokensTextBox.Text, out var maxTokens) || maxTokens < 100)
                {
                    MessageBox.Show("Max Tokens must be >= 100", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate base URL
                var baseUrl = BaseUrlTextBox.Text.Trim();
                if (string.IsNullOrEmpty(baseUrl))
                {
                    MessageBox.Show("Base URL cannot be empty", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update settings
                _aiSettings.Provider = ProviderComboBox.SelectedItem?.ToString() ?? "OpenAI";
                _aiSettings.Model = ModelTextBox.Text.Trim();
                _aiSettings.BaseUrl = baseUrl;
                _aiSettings.Temperature = TemperatureSlider.Value;
                _aiSettings.MaxRetries = maxRetries;
                _aiSettings.MaxTokens = maxTokens;

                // Store API key securely with provider-specific key
                var apiKey = ApiKeyPasswordBox.Password;
                var credentialKey = $"{_aiSettings.Provider}.ApiKey";
                
                if (!string.IsNullOrEmpty(apiKey))
                {
                    await _credentialService.StoreCredentialAsync(credentialKey, apiKey);
                    _aiSettings.ApiKey = apiKey;
                }

                StatusTextBlock.Text = "Settings saved successfully";
                _logger.LogInformation("AI settings saved for provider: {Provider}", _aiSettings.Provider);

                MessageBox.Show("Settings saved successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save settings: {ex.Message}");
                StatusTextBlock.Text = $"Error saving settings: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestConnectionButton.IsEnabled = false;
                StatusTextBlock.Text = "Testing connection...";

                var provider = ProviderComboBox.SelectedItem?.ToString() ?? "OpenAI";
                
                if (provider == "OpenAI")
                {
                    if (string.IsNullOrEmpty(ApiKeyPasswordBox.Password))
                    {
                        MessageBox.Show("API key is required for OpenAI", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        StatusTextBlock.Text = "API key required";
                        return;
                    }

                    // Create temporary client for testing
                    var tempSettings = new Core.AISettings
                    {
                        BaseUrl = BaseUrlTextBox.Text.Trim(),
                        Model = ModelTextBox.Text.Trim(),
                        ApiKey = ApiKeyPasswordBox.Password
                    };

                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        var openAILogger = App.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger<Application.OpenAIClient>();
                        var client = new Application.OpenAIClient(httpClient, tempSettings, openAILogger ?? throw new InvalidOperationException("Logger not available"));
                        var isHealthy = await client.HealthCheckAsync();

                        if (isHealthy)
                        {
                            StatusTextBlock.Text = "Connection successful!";
                            MessageBox.Show("OpenAI connection successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            StatusTextBlock.Text = "Connection failed";
                            MessageBox.Show("Failed to connect to OpenAI", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else if (provider == "Groq")
                {
                    if (string.IsNullOrEmpty(ApiKeyPasswordBox.Password))
                    {
                        MessageBox.Show("API key is required for Groq", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        StatusTextBlock.Text = "API key required";
                        return;
                    }

                    if (string.IsNullOrEmpty(ModelTextBox.Text))
                    {
                        MessageBox.Show("Model is required for Groq", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        StatusTextBlock.Text = "Model required";
                        return;
                    }

                    // Create temporary client for testing
                    var tempSettings = new Core.AISettings
                    {
                        Provider = "Groq",
                        BaseUrl = BaseUrlTextBox.Text.Trim(),
                        Model = ModelTextBox.Text.Trim(),
                        ApiKey = ApiKeyPasswordBox.Password,
                        TimeoutSeconds = 15
                    };

                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        var groqLogger = App.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger<Application.GroqClient>();
                        var client = new Application.GroqClient(httpClient, tempSettings, groqLogger ?? throw new InvalidOperationException("Logger not available"));
                        
                        try
                        {
                            var isHealthy = await client.HealthCheckAsync();

                            if (isHealthy)
                            {
                                StatusTextBlock.Text = "Connection successful!";
                                MessageBox.Show("Groq connection successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                StatusTextBlock.Text = "Connection failed - Invalid response from Groq";
                                MessageBox.Show("Failed to connect to Groq. Please verify your API key and base URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            StatusTextBlock.Text = $"Connection failed: {ex.Message}";
                            MessageBox.Show($"Network error: {ex.Message}\n\nPlease check your base URL and internet connection.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            _logger.LogError($"Groq connection test failed: {ex}");
                        }
                        catch (TaskCanceledException)
                        {
                            StatusTextBlock.Text = "Connection timeout";
                            MessageBox.Show("Connection timed out. Please check your internet connection and base URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else if (provider == "Ollama")
                {
                    // Test Ollama connection
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        var tempSettings = new Core.AISettings
                        {
                            BaseUrl = BaseUrlTextBox.Text.Trim(),
                            Model = ModelTextBox.Text.Trim()
                        };

                        var ollamaLogger = App.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger<Application.OllamaClient>();
                        var client = new Application.OllamaClient(httpClient, tempSettings, ollamaLogger ?? throw new InvalidOperationException("Logger not available"));
                        var isHealthy = await client.HealthCheckAsync();

                        if (isHealthy)
                        {
                            StatusTextBlock.Text = "Connection successful!";
                            MessageBox.Show("Ollama connection successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            StatusTextBlock.Text = "Connection failed";
                            MessageBox.Show("Failed to connect to Ollama", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Connection test failed: {ex.Message}");
                StatusTextBlock.Text = $"Connection failed: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
