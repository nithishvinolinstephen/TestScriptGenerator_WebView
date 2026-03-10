using TestScriptGeneratorTool.Domain;

namespace TestScriptGeneratorTool.Infrastructure
{
    /// <summary>
    /// Interface for locator engine that generates and validates locators.
    /// </summary>
    public interface ILocatorEngine
    {
        /// <summary>
        /// Generates locator definition with primary and alternative locators following priority rules.
        /// </summary>
        Task<LocatorDefinition> GenerateLocatorAsync(ElementDescriptor element, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a locator uniquely identifies the element in the DOM.
        /// </summary>
        Task<bool> ValidateLocatorUniquenessAsync(string locator, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets alternative locators for an element.
        /// </summary>
        List<string> GetAlternativeLocators(ElementDescriptor element);
    }
}
