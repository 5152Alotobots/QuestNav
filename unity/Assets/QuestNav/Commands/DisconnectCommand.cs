using System;
using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Network.NetworkTables_CSharp;
using QuestNav.Transformation;

namespace QuestNav.Commands
{
    /// <summary>
    /// Command to handle clean disconnection from the robot
    /// </summary>
    public class DisconnectCommand : QuestCommandBase
    {
        private readonly QuestNetworkManager networkManager;
        
        /// <summary>
        /// Command ID for disconnect
        /// </summary>
        public override int CommandId => QuestNavConstants.CMD_DISCONNECT;
        
        /// <summary>
        /// Descriptive name of the command
        /// </summary>
        public override string CommandName => "Disconnect";
        
        /// <summary>
        /// Creates a new disconnect command
        /// </summary>
        /// <param name="transformManager">Transform manager for pose operations</param>
        /// <param name="networkManager">Network manager for connection handling</param>
        public DisconnectCommand(QuestTransformManager transformManager, QuestNetworkManager networkManager) 
            : base(transformManager)
        {
            this.networkManager = networkManager;
        }
        
        /// <summary>
        /// Executes the disconnect command
        /// </summary>
        /// <param name="nt">NetworkTables interface</param>
        /// <returns>Response code to send back to the robot</returns>
        public override int Execute(Nt4Source nt)
        {
            try
            {
                LogCommand("Received disconnect request");
                
                // Send disconnect notification
                nt.PublishValue(QuestNavConstants.Topics.MISO, QuestNavConstants.RESP_DISCONNECT);
                
                // Wait a moment for the message to be sent
                System.Threading.Thread.Sleep(QuestNavConstants.MESSAGE_SEND_DELAY_MS);
                
                // Disconnect from the robot
                networkManager.Disconnect();
                
                LogCommand("Disconnect successful");
                
                // This response likely won't be received by the robot
                return QuestNavConstants.RESP_DISCONNECT;
            }
            catch (Exception ex)
            {
                LogError($"Error during disconnect: {ex.Message}");
                return QuestNavConstants.RESP_ERROR;
            }
        }
        
        /// <summary>
        /// Disconnect can execute even during a reset operation
        /// </summary>
        public override bool CanExecute(bool resetInProgress)
        {
            return true; // Disconnect can always execute
        }
    }
}