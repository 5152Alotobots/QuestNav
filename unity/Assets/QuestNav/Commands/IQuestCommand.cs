using QuestNav.Network.NetworkTables_CSharp;

namespace QuestNav.Commands
{
    /// <summary>
    /// Interface for all robot commands processed by the Quest
    /// </summary>
    public interface IQuestCommand
    {
        /// <summary>
        /// Unique command identifier
        /// </summary>
        int CommandId { get; }
        
        /// <summary>
        /// Descriptive name of the command
        /// </summary>
        string CommandName { get; }
        
        /// <summary>
        /// Executes the command
        /// </summary>
        /// <param name="nt">NetworkTables interface</param>
        /// <returns>Response code to send back to the robot</returns>
        int Execute(Nt4Source nt);
        
        /// <summary>
        /// Checks if this command can be executed in the current state
        /// </summary>
        /// <param name="resetInProgress">Whether a reset operation is in progress</param>
        /// <returns>True if the command can be executed</returns>
        bool CanExecute(bool resetInProgress);
    }
}