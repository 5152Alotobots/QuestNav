using System;
using QuestNav.Core;
using QuestNav.Network.NetworkTables_CSharp;
using QuestNav.Transformation;

namespace QuestNav.Commands
{
    /// <summary>
    /// Command to reset the heading of the Quest without changing position
    /// </summary>
    public class HeadingResetCommand : QuestCommandBase
    {
        /// <summary>
        /// Command ID for heading reset
        /// </summary>
        public override int CommandId => QuestNavConstants.CMD_HEADING_RESET;
        
        /// <summary>
        /// Descriptive name of the command
        /// </summary>
        public override string CommandName => "HeadingReset";
        
        /// <summary>
        /// Creates a new heading reset command
        /// </summary>
        /// <param name="transformManager">Transform manager for pose operations</param>
        public HeadingResetCommand(QuestTransformManager transformManager) 
            : base(transformManager)
        {
        }
        
        /// <summary>
        /// Executes the heading reset command
        /// </summary>
        /// <param name="nt">NetworkTables interface</param>
        /// <returns>Response code to send back to the robot</returns>
        public override int Execute(Nt4Source nt)
        {
            try
            {
                LogCommand("Received heading reset request, initiating recenter...");
                
                if (transformManager.RecenterPlayer())
                {
                    LogCommand("Heading reset successful");
                    return QuestNavConstants.RESP_HEADING_RESET_SUCCESS;
                }
                else
                {
                    LogError("Heading reset failed");
                    return QuestNavConstants.RESP_NONE;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during heading reset: {ex.Message}");
                return QuestNavConstants.RESP_NONE;
            }
        }
    }
}