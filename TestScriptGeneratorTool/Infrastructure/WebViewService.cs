using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Windows.Controls;
using System.IO;
using System.Text.Json;

namespace TestScriptGeneratorTool.Infrastructure
{
    /// <summary>
    /// Service for managing WebView2 initialization and control.
    /// </summary>
    public class WebViewService
    {
        private readonly ILogger<WebViewService> _logger;
        private WebView2? _webView;
        private TaskCompletionSource<bool> _initializationTcs = new();
        private bool _selectionModeEnabled = false;

        public event EventHandler<string>? NavigationCompleted;
        public event EventHandler<string>? NavigationFailed;
        public event EventHandler<ElementInfo>? ElementSelected;

        public WebViewService(ILogger<WebViewService> logger)
        {
            _logger = logger;
            _logger.LogInformation("WebViewService created");
        }

        /// <summary>
        /// Initializes WebView2 control asynchronously.
        /// </summary>
        public async Task InitializeWebViewAsync(WebView2 webView)
        {
            try
            {
                _webView = webView;
                _logger.LogInformation("WebView2 initialization started");

                // Initialize WebView2 environment
                var userDataFolder = Path.Combine(Path.GetTempPath(), "TestScriptGenerator");
                Directory.CreateDirectory(userDataFolder);

                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await _webView.EnsureCoreWebView2Async(environment);

                // Subscribe to navigation events
                _webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                _webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                _logger.LogInformation("WebView2 initialization completed successfully");
                _initializationTcs.SetResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"WebView2 initialization failed: {ex.Message}");
                _initializationTcs.SetException(ex);
                NavigationFailed?.Invoke(this, ex.Message);
            }
        }

        /// <summary>
        /// Navigates to the specified URL.
        /// </summary>
        public void Navigate(string url)
        {
            if (_webView?.CoreWebView2 == null)
            {
                _logger.LogWarning("WebView2 not initialized. Cannot navigate.");
                return;
            }

            try
            {
                _logger.LogInformation($"Navigating to: {url}");
                _webView.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Navigation failed: {ex.Message}");
                NavigationFailed?.Invoke(this, ex.Message);
            }
        }

        /// <summary>
        /// Executes JavaScript code in the WebView2.
        /// </summary>
        public async Task<string> ExecuteScriptAsync(string script)
        {
            if (_webView?.CoreWebView2 == null)
            {
                _logger.LogWarning("WebView2 not initialized. Cannot execute script.");
                return "";
            }

            try
            {
                var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
                _logger.LogInformation($"Script executed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Script execution failed: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Enables element selection mode.
        /// </summary>
        public async Task EnableElementSelectionAsync()
        {
            if (_webView?.CoreWebView2 == null)
            {
                _logger.LogWarning("WebView2 not initialized. Cannot enable selection.");
                return;
            }

            try
            {
                _logger.LogInformation("Enabling element selection mode");
                await ExecuteScriptAsync(ElementInspectionScripts.EnableSelectionMode);
                _selectionModeEnabled = true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to enable selection: {ex.Message}");
            }
        }

        /// <summary>
        /// Disables element selection mode.
        /// </summary>
        public async Task DisableElementSelectionAsync()
        {
            if (_webView?.CoreWebView2 == null)
            {
                _logger.LogWarning("WebView2 not initialized. Cannot disable selection.");
                return;
            }

            try
            {
                _logger.LogInformation("Disabling element selection mode");
                await ExecuteScriptAsync(ElementInspectionScripts.DisableSelectionMode);
                _selectionModeEnabled = false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to disable selection: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets whether element selection mode is enabled.
        /// </summary>
        public bool IsSelectionModeEnabled => _selectionModeEnabled;

        /// <summary>
        /// Waits for WebView2 to be initialized.
        /// </summary>
        public Task WaitForInitializationAsync()
        {
            return _initializationTcs.Task;
        }

        private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                _logger.LogInformation("Navigation completed successfully");
                NavigationCompleted?.Invoke(this, _webView?.Source?.ToString() ?? "");
            }
            else
            {
                _logger.LogWarning($"Navigation failed with code: {e.WebErrorStatus}");
                NavigationFailed?.Invoke(this, e.WebErrorStatus.ToString());
            }
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                _logger.LogInformation($"Web message received: {message}");

                if (string.IsNullOrEmpty(message))
                {
                    _logger.LogWarning("Received empty message from WebView2");
                    return;
                }

                var jsonDoc = JsonDocument.Parse(message);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("type", out var typeElement))
                {
                    var typeValue = typeElement.GetString();
                    _logger.LogInformation($"Message type: {typeValue}");

                    if (typeValue == "elementSelected")
                    {
                        if (root.TryGetProperty("data", out var dataElement))
                        {
                            var elementInfo = new ElementInfo
                            {
                                Type = dataElement.TryGetProperty("type", out var typeElem) ? typeElem.GetString() ?? "" : "",
                                Id = dataElement.TryGetProperty("id", out var idElem) ? idElem.GetString() ?? "" : "",
                                ClassName = dataElement.TryGetProperty("className", out var classElem) ? classElem.GetString() ?? "" : "",
                                Text = dataElement.TryGetProperty("text", out var textElem) ? textElem.GetString() ?? "" : "",
                                Selector = dataElement.TryGetProperty("selector", out var selectorElem) ? selectorElem.GetString() ?? "" : "",
                                Attributes = dataElement.TryGetProperty("attributes", out var attrsElem) ? ParseAttributes(attrsElem) : new()
                            };

                            _logger.LogInformation($"Element selected: {elementInfo.Type} - {elementInfo.Selector}");
                            ElementSelected?.Invoke(this, elementInfo);
                        }
                        else
                        {
                            _logger.LogWarning("Message has type 'elementSelected' but no 'data' property");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Message does not have a 'type' property");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing web message: {ex.Message}\nStack: {ex.StackTrace}");
            }
        }

        private Dictionary<string, string> ParseAttributes(JsonElement attributesElement)
        {
            var attributes = new Dictionary<string, string>();
            if (attributesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in attributesElement.EnumerateObject())
                {
                    attributes[property.Name] = property.Value.GetString() ?? "";
                }
            }
            return attributes;
        }
    }

    /// <summary>
    /// Information about a selected element.
    /// </summary>
    public class ElementInfo
    {
        public string Type { get; set; } = "";
        public string Id { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string Text { get; set; } = "";
        public string Selector { get; set; } = "";
        public Dictionary<string, string> Attributes { get; set; } = new();
    }
}
