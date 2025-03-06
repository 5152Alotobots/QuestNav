using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Telemetry;
using QuestNav.Transformation;
using System;
using System.Threading;
using UnityEngine;

namespace QuestNav.Commands
{
    /// <summary>
    /// Command that resets the pose (position and orientation) of the VR headset
    /// </summary>
    public class CommandPoseReset : ICommand
    {
        private readonly NetworkTableManager networkTableManager;
        private readonly PoseManager poseManager;
        
        public long ResponseCode => QuestNavConstants.Commands.POSE_RESET_COMPLETE;
        public long CommandId => QuestNavConstants.Commands.POSE_RESET;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="networkTableManager">Reference to NetworkTableManager</param>
        /// <param name="poseManager">Reference to PoseManager</param>
        public CommandPoseReset(NetworkTableManager networkTableManager, PoseManager poseManager)
        {
            this.networkTableManager = networkTableManager;
            this.poseManager = poseManager;
        }
        
        /// <summary>
        /// Execute the pose reset command
        /// </summary>
        /// <returns>True if complete, false if still in progress</returns>
        public bool Execute()
        {
            QueuedLogger.Log("[CommandPoseReset] Executing pose reset");
            
            try
            {
                // Read pose data from NetworkTables with retry logic
                double[] resetPose = null;
                bool success = false;
                int attemptCount = 0;
                
                // Attempt to read pose data from NetworkTables with retry logic
                for (int i = 0; i < QuestNavConstants.Thresholds.MAX_COMMAND_RETRIES && !success; i++)
                {
                    attemptCount++;
                    
                    // Add delay between retries to allow for network latency
                    if (i > 0)
                    {
                        Thread.Sleep((int)QuestNavConstants.Thresholds.COMMAND_RETRY_DELAY_MS);
                        QueuedLogger.Log($"[CommandPoseReset] Attempt {attemptCount} of {QuestNavConstants.Thresholds.MAX_COMMAND_RETRIES}...");
                    }
                    
                    QueuedLogger.Log($"[CommandPoseReset] Reading NetworkTables Values (Attempt {attemptCount}):");
                    
                    // Read the pose array from NetworkTables
                    // Format: [X, Y, Rotation] in FRC field coordinates
                    resetPose = networkTableManager.GetValue<double[]>(QuestNavConstants.Topics.COMMAND_RESETPOSE);
                    
                    // Validate pose data format and field boundaries
                    if (resetPose != null && resetPose.Length == 3)
                    {
                        // Check if pose is within valid field boundaries
                        if (resetPose[0] < QuestNavConstants.FieldLimits.MIN_FIELD_X || 
                            resetPose[0] > QuestNavConstants.FieldLimits.FIELD_LENGTH ||
                            resetPose[1] < QuestNavConstants.FieldLimits.MIN_FIELD_Y || 
                            resetPose[1] > QuestNavConstants.FieldLimits.FIELD_WIDTH)
                        {
                            QueuedLogger.LogWarning($"[CommandPoseReset] Reset pose outside field boundaries: X:{resetPose[0]:F3} Y:{resetPose[1]:F3}");
                            continue;
                        }
                        success = true;
                        QueuedLogger.Log($"[CommandPoseReset] Successfully read reset pose values on attempt {attemptCount}");
                    }
                    
                    QueuedLogger.Log($"[CommandPoseReset] Values (Attempt {attemptCount}): " +
                                   $"X:{resetPose?[0]:F3} Y:{resetPose?[1]:F3} Rot:{resetPose?[2]:F3}");
                }
                
                // Exit if we couldn't get valid pose data
                if (!success)
                {
                    QueuedLogger.LogWarning($"[CommandPoseReset] Failed to read valid reset pose values after {attemptCount} attempts");
                    return true; // Return true to complete the command even though it failed
                }
                
                // Extract pose components from the array
                float resetX = (float)resetPose[0];          // FRC X coordinate (along length of field)
                float resetY = (float)resetPose[1];          // FRC Y coordinate (along width of field)
                float resetRotation = (float)resetPose[2];   // FRC rotation (CCW positive)
                
                // Normalize rotation to -180 to 180 degrees range
                resetRotation = CoordinateConverter.NormalizeAngle180(resetRotation);
                
                // Perform the actual pose reset
                success = poseManager.ResetPose(resetX, resetY, resetRotation);
                
                if (success)
                {
                    QueuedLogger.Log("[CommandPoseReset] Pose reset completed successfully");
                }
                else
                {
                    QueuedLogger.LogError("[CommandPoseReset] Pose reset failed during execution");
                }
                
                return true; // Command is complete
            }
            catch (Exception e)
            {
                QueuedLogger.LogError($"[CommandPoseReset] Error during pose reset: {e.Message}");
                QueuedLogger.LogException(e);
                return true; // Return true to complete the command even though it failed
            }
        }
    }
}