using System.Collections.Generic;
using QuestNav.Network;
using QuestNav.Transformation;

namespace QuestNav.Commands
{
    /// <summary>
    /// Factory for creating command instances
    /// </summary>
    public class QuestCommandFactory
    {
        private readonly Dictionary<int, IQuestCommand> commandCache = new Dictionary<int, IQuestCommand>();
        private readonly QuestTransformManager transformManager;
        private readonly QuestNetworkManager networkManager;
        
        /// <summary>
        /// Creates a new command factory
        /// </summary>
        /// <param name="transformManager">Transform manager for pose operations</param>
        /// <param name="networkManager">Network manager for connection handling</param>
        public QuestCommandFactory(QuestTransformManager transformManager, QuestNetworkManager networkManager)
        {
            this.transformManager = transformManager;
            this.networkManager = networkManager;
            InitializeCommands();
        }
        
        /// <summary>
        /// Initializes and caches all command instances
        /// </summary>
        private void InitializeCommands()
        {
            // Create all command instances
            RegisterCommand(new HeadingResetCommand(transformManager));
            RegisterCommand(new PoseResetCommand(transformManager));
            RegisterCommand(new PingCommand(transformManager));
            RegisterCommand(new TransformUpdateCommand(transformManager));
            RegisterCommand(new DisconnectCommand(transformManager, networkManager));
        }
        
        /// <summary>
        /// Registers a command in the command cache
        /// </summary>
        /// <param name="command">Command to register</param>
        private void RegisterCommand(IQuestCommand command)
        {
            commandCache[command.CommandId] = command;
        }
        
        /// <summary>
        /// Gets a command instance for the given command ID
        /// </summary>
        /// <param name="commandId">Command ID</param>
        /// <returns>Command instance or null if not found</returns>
        public IQuestCommand GetCommand(int commandId)
        {
            if (commandCache.TryGetValue(commandId, out var command))
            {
                return command;
            }
            return null;
        }
    }
}