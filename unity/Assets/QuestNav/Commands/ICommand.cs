namespace QuestNav.Commands
{
    /// <summary>
    /// Interface for all command handlers
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Execute the command
        /// </summary>
        /// <returns>True if command execution is complete, false if still in progress</returns>
        bool Execute();
        
        /// <summary>
        /// Response code to send back to robot when command completes
        /// </summary>
        long ResponseCode { get; }
        
        /// <summary>
        /// Command ID
        /// </summary>
        long CommandId { get; }
    }
}