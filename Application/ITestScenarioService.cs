using TestScriptGeneratorTool.Domain;

namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// Service for managing test scenarios and steps.
    /// </summary>
    public interface ITestScenarioService
    {
        void CreateScenario(string name, string description);
        void AddStep(TestStep step);
        void RemoveStep(string stepId);
        List<TestScenario> GetAllScenarios();
        TestScenario? GetCurrentScenario();
        void SetCurrentScenario(string scenarioId);
    }

    /// <summary>
    /// Implementation of ITestScenarioService.
    /// </summary>
    public class TestScenarioService : ITestScenarioService
    {
        private readonly List<TestScenario> _scenarios = new();
        private TestScenario? _currentScenario;

        public void CreateScenario(string name, string description)
        {
            var scenario = new TestScenario
            {
                Name = name,
                Description = description
            };
            _scenarios.Add(scenario);
            _currentScenario = scenario;
        }

        public void AddStep(TestStep step)
        {
            if (_currentScenario == null)
                throw new InvalidOperationException("No scenario selected");

            step.Order = _currentScenario.Steps.Count + 1;
            _currentScenario.Steps.Add(step);
            _currentScenario.UpdatedAt = DateTime.Now;
        }

        public void RemoveStep(string stepId)
        {
            if (_currentScenario == null)
                return;

            var step = _currentScenario.Steps.FirstOrDefault(s => s.Id == stepId);
            if (step != null)
            {
                _currentScenario.Steps.Remove(step);
                _currentScenario.UpdatedAt = DateTime.Now;
            }
        }

        public List<TestScenario> GetAllScenarios()
        {
            return _scenarios;
        }

        public TestScenario? GetCurrentScenario()
        {
            return _currentScenario;
        }

        public void SetCurrentScenario(string scenarioId)
        {
            _currentScenario = _scenarios.FirstOrDefault(s => s.Id == scenarioId);
        }
    }
}
