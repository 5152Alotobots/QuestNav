using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Telemetry;
using QuestNav.Transformation;
using UnityEngine;

namespace QuestNav.Commands
{
    /// <summary>
    /// Factory for creating command instances based on command ID
    /// </summary>
    public class CommandFactory : MonoBehaviour
    {
        /// <summary>
        /// Creates a command instance based on command ID
        /// </summary>
        /// <param name="commandId">The command ID to create</param>
        /// <param name="networkTableManager">NetworkTableManager dependency</param>
        /// <param name="poseManager">PoseManager dependency</param>
        /// <returns>Command instance or null if invalid command ID</returns>
        public static ICommand CreateCommand(long commandId, NetworkTableManager networkTableManager, PoseManager poseManager)
        {
            switch (commandId)
            {
                case QuestNavConstants.Commands.HEADING_RESET:
                    QueuedLogger.Log("[CommandFactory] Creating HeadingReset command");
                    return new CommandHeadingReset(poseManager);
                    
                case QuestNavConstants.Commands.POSE_RESET:
                    QueuedLogger.Log("[CommandFactory] Creating PoseReset command");
                    return new CommandPoseReset(networkTableManager, poseManager);
                    
                case QuestNavConstants.Commands.PING:
                    QueuedLogger.Log("[CommandFactory] Creating Ping command");
                    return new CommandPing();
                    
                default:
                    QueuedLogger.LogWarning($"[CommandFactory] Unknown command ID: {commandId}");
                    return null;
            }
        }
    }
}