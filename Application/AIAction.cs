namespace TestScriptGeneratorTool.Application
{
    /// <summary>
    /// Represents an action to be performed in AI generation mode.
    /// </summary>
    public class AIAction
    {
        /// <summary>
        /// Unique identifier for the action.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Description of the action to be performed.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the action was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets a display string for the action.
        /// </summary>
        public override string ToString()
        {
            return Description;
        }
    }
}
