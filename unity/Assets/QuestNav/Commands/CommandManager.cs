using UnityEngine;
using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Telemetry;
using QuestNav.Transformation;

namespace QuestNav.Commands
{
    /// <summary>
    /// Manages command processing and execution
    /// </summary>
    public class CommandManager : MonoBehaviour
    {
        private NetworkTableManager networkTableManager;
        private PoseManager poseManager;
        private Transform vrCamera;
        private Transform vrCameraRoot;
        private Transform resetTransform;
        
        /// <summary>
        /// Current command being executed
        /// </summary>
        private ICommand currentCommand = null;
        
        /// <summary>
        /// Flag indicating if a command is in progress
        /// </summary>
        private bool commandInProgress = false;
        
        /// <summary>
        /// Initialize the command manager
        /// </summary>
        public void Initialize(NetworkTableManager networkManager, Transform vrCamera, Transform vrCameraRoot, Transform resetTransform)
        {
            this.networkTableManager = networkManager;
            this.vrCamera = vrCamera;
            this.vrCameraRoot = vrCameraRoot;
            this.resetTransform = resetTransform;
            
            // Get the PoseManager component
            this.poseManager = GetComponent<PoseManager>();
            if (poseManager == null)
            {
                poseManager = gameObject.AddComponent<PoseManager>();
                poseManager.Initialize(vrCamera, vrCameraRoot, resetTransform);
            }
            
            QueuedLogger.Log("[CommandManager] Initialized");
        }
        
        /// <summary>
        /// Process commands from NetworkTables
        /// </summary>
        public void ProcessCommands()
        {
            if (!networkTableManager.IsConnected()) return;
            
            // Get the current command from NetworkTables
            long commandId = networkTableManager.GetValue<long>(QuestNavConstants.Topics.COMMAND_MOSI);
            
            // If a reset is in progress and the command has been cleared
            if (commandInProgress && commandId == 0)
            {
                QueuedLogger.Log("[CommandManager] Command completed and acknowledged by robot");
                commandInProgress = false;
                currentCommand = null;
                return;
            }
            
            // If we're already processing a command, continue with execution
            if (commandInProgress)
            {
                ContinueCommandExecution();
                return;
            }
            
            // Skip if command ID is 0 (no command)
            if (commandId == 0)
            {
                networkTableManager.PublishValue(QuestNavConstants.Topics.COMMAND_MISO, QuestNavConstants.Commands.IDLE);
                return;
            }
            
            // Create a new command instance
            currentCommand = CommandFactory.CreateCommand(commandId, networkTableManager, poseManager);
            
            // Execute the command if valid
            if (currentCommand != null)
            {
                QueuedLogger.Log($"[CommandManager] Executing command ID: {commandId}");
                commandInProgress = true;
                ContinueCommandExecution();
            }
        }
        
        /// <summary>
        /// Continue execution of the current command
        /// </summary>
        private void ContinueCommandExecution()
        {
            if (currentCommand == null)
            {
                commandInProgress = false;
                return;
            }
            
            // Execute the command and check if complete
            bool isComplete = currentCommand.Execute();
            
            // If command execution is complete, send response
            if (isComplete)
            {
                QueuedLogger.Log($"[CommandManager] Command {currentCommand.CommandId} completed with response code {currentCommand.ResponseCode}");
                networkTableManager.PublishValue(QuestNavConstants.Topics.COMMAND_MISO, currentCommand.ResponseCode);
            }
        }
    }
}