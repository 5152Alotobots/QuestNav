using QuestNav.Network.NetworkTables_CSharp;
using QuestNav.Transformation;
using UnityEngine;

namespace QuestNav.Commands
{
    /// <summary>
    /// Base class for all Quest commands with common functionality
    /// </summary>
    public abstract class QuestCommandBase : IQuestCommand
    {
        // Dependencies
        protected readonly QuestTransformManager transformManager;
        
        /// <summary>
        /// Unique command identifier
        /// </summary>
        public abstract int CommandId { get; }
        
        /// <summary>
        /// Descriptive name of the command
        /// </summary>
        public abstract string CommandName { get; }
        
        /// <summary>
        /// Creates a new command with required dependencies
        /// </summary>
        /// <param name="transformManager">Transform manager for pose operations</param>
        protected QuestCommandBase(QuestTransformManager transformManager)
        {
            this.transformManager = transformManager;
        }
        
        /// <summary>
        /// Executes the command
        /// </summary>
        /// <param name="nt">NetworkTables interface</param>
        /// <returns>Response code to send back to the robot</returns>
        public abstract int Execute(Nt4Source nt);
        
        /// <summary>
        /// Checks if this command can be executed in the current state
        /// </summary>
        /// <param name="resetInProgress">Whether a reset operation is in progress</param>
        /// <returns>True if the command can be executed</returns>
        public virtual bool CanExecute(bool resetInProgress)
        {
            // Most commands cannot execute during a reset
            return !resetInProgress;
        }
        
        /// <summary>
        /// Logs command execution with consistent formatting
        /// </summary>
        /// <param name="message">Message to log</param>
        protected void LogCommand(string message)
        {
            Debug.Log($"[{CommandName}] {message}");
        }
        
        /// <summary>
        /// Logs command errors with consistent formatting
        /// </summary>
        /// <param name="message">Error message to log</param>
        protected void LogError(string message)
        {
            Debug.LogError($"[{CommandName}] ERROR: {message}");
        }
    }
}