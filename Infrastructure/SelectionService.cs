using Microsoft.Extensions.Logging;
using TestScriptGeneratorTool.Domain;

namespace TestScriptGeneratorTool.Infrastructure
{
    /// <summary>
    /// Service for managing test element selection state.
    /// </summary>
    public class SelectionService : ISelectionService
    {
        private readonly ILogger<SelectionService> _logger;
        private TestElement? _currentSelection;
        private readonly List<TestElement> _selections = new();

        public event EventHandler<TestElement>? ElementSelected;
        public event EventHandler? SelectionCleared;

        public SelectionService(ILogger<SelectionService> logger)
        {
            _logger = logger;
            _logger.LogInformation("SelectionService initialized");
        }

        public void SelectElement(TestElement element)
        {
            _currentSelection = element;
            _selections.Add(element);
            _logger.LogInformation($"Element selected: {element.ElementType} - {element.Selector}");
            ElementSelected?.Invoke(this, element);
        }

        public void ClearSelection()
        {
            _currentSelection = null;
            _logger.LogInformation("Selection cleared");
            SelectionCleared?.Invoke(this, EventArgs.Empty);
        }

        public TestElement? GetCurrentSelection()
        {
            return _currentSelection;
        }

        public IReadOnlyList<TestElement> GetAllSelections()
        {
            return _selections.AsReadOnly();
        }
    }
}
