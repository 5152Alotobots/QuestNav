using System;
using QuestNav.Core;
using QuestNav.Network.NetworkTables_CSharp;
using QuestNav.Transformation;
using QuestNavigation.Commands;

namespace QuestNav.Commands
{
    /// <summary>
    /// Command to perform a full pose reset of the Quest
    /// </summary>
    public class PoseResetCommand : QuestCommandBase
    {
        /// <summary>
        /// Command ID for pose reset
        /// </summary>
        public override int CommandId => QuestNavConstants.CMD_POSE_RESET;
        
        /// <summary>
        /// Descriptive name of the command
        /// </summary>
        public override string CommandName => "PoseReset";
        
        /// <summary>
        /// Creates a new pose reset command
        /// </summary>
        /// <param name="transformManager">Transform manager for pose operations</param>
        public PoseResetCommand(QuestTransformManager transformManager) 
            : base(transformManager)
        {
        }
        
        /// <summary>
        /// Executes the pose reset command
        /// </summary>
        /// <param name="nt">NetworkTables interface</param>
        /// <returns>Response code to send back to the robot</returns>
        public override int Execute(Nt4Source nt)
        {
            try
            {
                LogCommand("Received pose reset request, initiating reset...");
                
                if (transformManager.ProcessPoseReset(nt))
                {
                    LogCommand("Pose reset successful");
                    return QuestNavConstants.RESP_POSE_RESET_SUCCESS;
                }
                else
                {
                    LogError("Pose reset failed");
                    return QuestNavConstants.RESP_NONE;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during pose reset: {ex.Message}");
                return QuestNavConstants.RESP_NONE;
            }
        }
    }
}