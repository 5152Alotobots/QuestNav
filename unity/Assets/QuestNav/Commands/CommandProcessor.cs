using System;
using QuestNav.Core;
using QuestNav.Network;
using UnityEngine;

namespace QuestNav.Commands
{
    /// <summary>
    /// Processes commands received from the robot
    /// </summary>
    public class CommandProcessor
    {
        // Dependencies
        private readonly QuestNetworkManager networkManager;
        private readonly QuestCommandFactory commandFactory;
        
        // State
        private long currentCommand = 0;
        private bool resetInProgress = false;
        
        /// <summary>
        /// Creates a new command processor
        /// </summary>
        /// <param name="networkManager">Network manager for NT access</param>
        /// <param name="commandFactory">Factory for creating command instances</param>
        public CommandProcessor(QuestNetworkManager networkManager, QuestCommandFactory commandFactory)
        {
            this.networkManager = networkManager;
            this.commandFactory = commandFactory;
        }
        
        /// <summary>
        /// Processes the current command from the robot
        /// </summary>
        public void ProcessCommands()
        {
            try
            {
                var nt = networkManager.NetworkTables;
                if (nt == null) return;
                
                // Get the latest command
                currentCommand = nt.GetLong(QuestNavConstants.Topics.MOSI);
                networkManager.UpdateReceiveTime();
                
                // Check if a reset operation completed
                if (resetInProgress && currentCommand == QuestNavConstants.CMD_NONE)
                {
                    resetInProgress = false;
                    Debug.Log("[CommandProcessor] Reset operation completed");
                    return;
                }

                // Get command instance
                var command = commandFactory.GetCommand((int)currentCommand);
                
                // Process the command if found and can execute
                if (command != null && command.CanExecute(resetInProgress))
                {
                    // Execute the command and get response
                    int response = command.Execute(nt);
                    
                    // Send response to robot
                    nt.PublishValue(QuestNavConstants.Topics.MISO, response);
                    
                    // Update reset state for commands that start a reset operation
                    if (command.CommandId == QuestNavConstants.CMD_HEADING_RESET || 
                        command.CommandId == QuestNavConstants.CMD_POSE_RESET)
                    {
                        if (response != QuestNavConstants.RESP_NONE)
                        {
                            resetInProgress = true;
                        }
                    }
                }
                else if (currentCommand != QuestNavConstants.CMD_NONE && !resetInProgress)
                {
                    // Unknown command or cannot execute
                    nt.PublishValue(QuestNavConstants.Topics.MISO, QuestNavConstants.RESP_NONE);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandProcessor] Error processing commands: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks if a reset operation is in progress
        /// </summary>
        public bool IsResetInProgress()
        {
            return resetInProgress;
        }
    }
}