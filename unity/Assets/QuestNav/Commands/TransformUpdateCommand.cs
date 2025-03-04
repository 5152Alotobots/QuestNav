using System;
using QuestNav.Core;
using QuestNav.Network.NetworkTables_CSharp;
using QuestNav.Transformation;
using QuestNavigation.Commands;

namespace QuestNav.Commands
{
    /// <summary>
    /// Command to update the transformation matrix based on robot pose
    /// </summary>
    public class TransformUpdateCommand : QuestCommandBase
    {
        /// <summary>
        /// Command ID for transform update
        /// </summary>
        public override int CommandId => QuestNavConstants.CMD_TRANSFORM_UPDATE;
        
        /// <summary>
        /// Descriptive name of the command
        /// </summary>
        public override string CommandName => "TransformUpdate";
        
        /// <summary>
        /// Creates a new transform update command
        /// </summary>
        /// <param name="transformManager">Transform manager for pose operations</param>
        public TransformUpdateCommand(QuestTransformManager transformManager) 
            : base(transformManager)
        {
        }
        
        /// <summary>
        /// Executes the transform update command
        /// </summary>
        /// <param name="nt">NetworkTables interface</param>
        /// <returns>Response code to send back to the robot</returns>
        public override int Execute(Nt4Source nt)
        {
            try
            {
                double[] frcPose = nt.GetDoubleArray(QuestNavConstants.Topics.UPDATE_TRANSFORM);
                if (frcPose == null || frcPose.Length != 3)
                {
                    LogError("Invalid transform data received");
                    return QuestNavConstants.RESP_NONE;
                }
                
                // Extract FRC coordinates
                float frcX = (float)frcPose[0];
                float frcY = (float)frcPose[1];
                float frcRotation = (float)frcPose[2];
                
                LogCommand($"Received transform update: X:{frcX:F2} Y:{frcY:F2} Rot:{frcRotation:F2}°");
                
                // Update transform
                if (transformManager.UpdateTransformFromFrcCoordinates(frcX, frcY, frcRotation))
                {
                    LogCommand("Transform update successful");
                    return QuestNavConstants.RESP_TRANSFORM_SUCCESS;
                }
                else
                {
                    LogError("Transform update failed - invalid coordinates");
                    return QuestNavConstants.RESP_NONE;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during transform update: {ex.Message}");
                return QuestNavConstants.RESP_NONE;
            }
        }
    }
}