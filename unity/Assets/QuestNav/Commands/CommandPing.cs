using QuestNav.Core;
using QuestNav.Telemetry;

namespace QuestNav.Commands
{
    /// <summary>
    /// Command that responds to a ping request from the robot
    /// </summary>
    public class CommandPing : ICommand
    {
        public long ResponseCode => QuestNavConstants.Commands.PING_RESPONSE;
        public long CommandId => QuestNavConstants.Commands.PING;
        
        /// <summary>
        /// Execute the ping command
        /// </summary>
        /// <returns>True as ping always completes immediately</returns>
        public bool Execute()
        {
            QueuedLogger.Log("[CommandPing] Ping received, responding...");
            return true; // Ping always completes immediately
        }
    }
}