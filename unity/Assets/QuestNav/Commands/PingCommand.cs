using QuestNav.Core;
using QuestNav.Network.NetworkTables_CSharp;
using QuestNav.Transformation;
using QuestNavigation.Commands;

namespace QuestNav.Commands
{
    /// <summary>
    /// Command to respond to a ping request from the robot
    /// </summary>
    public class PingCommand : QuestCommandBase
    {
        /// <summary>
        /// Command ID for ping
        /// </summary>
        public override int CommandId => QuestNavConstants.CMD_PING;
        
        /// <summary>
        /// Descriptive name of the command
        /// </summary>
        public override string CommandName => "Ping";
        
        /// <summary>
        /// Creates a new ping command
        /// </summary>
        /// <param name="transformManager">Transform manager for pose operations</param>
        public PingCommand(QuestTransformManager transformManager) 
            : base(transformManager)
        {
        }
        
        /// <summary>
        /// Executes the ping command
        /// </summary>
        /// <param name="nt">NetworkTables interface</param>
        /// <returns>Response code to send back to the robot</returns>
        public override int Execute(Nt4Source nt)
        {
            LogCommand("Ping received, responding...");
            return QuestNavConstants.RESP_PING_RESPONSE;
        }
        
        /// <summary>
        /// Ping can execute even during a reset operation
        /// </summary>
        public override bool CanExecute(bool resetInProgress)
        {
            return true; // Ping can always execute
        }
    }
}