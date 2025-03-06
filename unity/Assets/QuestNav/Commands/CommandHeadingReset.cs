using QuestNav.Core;
using QuestNav.Telemetry;
using QuestNav.Transformation;

namespace QuestNav.Commands
{
    /// <summary>
    /// Command that resets the heading of the VR headset
    /// </summary>
    public class CommandHeadingReset : ICommand
    {
        private readonly PoseManager poseManager;
        
        public long ResponseCode => QuestNavConstants.Commands.HEADING_RESET_COMPLETE;
        public long CommandId => QuestNavConstants.Commands.HEADING_RESET;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="poseManager">Reference to PoseManager</param>
        public CommandHeadingReset(PoseManager poseManager)
        {
            this.poseManager = poseManager;
        }
        
        /// <summary>
        /// Execute the heading reset command
        /// </summary>
        /// <returns>True if complete, false if still in progress</returns>
        public bool Execute()
        {
            QueuedLogger.Log("[CommandHeadingReset] Executing heading reset");
            
            // Perform the heading reset using PoseManager
            bool success = poseManager.RecenterHeading();
            
            if (success)
            {
                QueuedLogger.Log("[CommandHeadingReset] Heading reset completed successfully");
            }
            else
            {
                QueuedLogger.LogError("[CommandHeadingReset] Heading reset failed");
            }
            
            // This command always completes immediately
            return true;
        }
    }
}