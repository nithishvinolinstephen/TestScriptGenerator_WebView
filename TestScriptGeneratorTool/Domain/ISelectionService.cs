namespace TestScriptGeneratorTool.Domain
{
    /// <summary>
    /// Service interface for managing test element selection.
    /// </summary>
    public interface ISelectionService
    {
        event EventHandler<TestElement>? ElementSelected;
        event EventHandler? SelectionCleared;

        void SelectElement(TestElement element);
        void ClearSelection();
        TestElement? GetCurrentSelection();
        IReadOnlyList<TestElement> GetAllSelections();
    }
}
