using System.Collections;
using UnityEngine;
using QuestNav.Commands;
using QuestNav.Network;
using QuestNav.Telemetry;
using QuestNav.Transformation;
using QuestNav.UI;

namespace QuestNav.Core
{
    /// <summary>
    /// Main component that coordinates all QuestNav subsystems
    /// </summary>
    public class QuestNavCore : MonoBehaviour
    {
        #region Unity References
        /// <summary>
        /// Reference to the OVR Camera Rig for tracking
        /// </summary>
        public OVRCameraRig cameraRig;

        /// <summary>
        /// Reference to the VR camera transform
        /// </summary>
        [SerializeField] 
        public Transform vrCamera;

        /// <summary>
        /// Reference to the VR camera root transform
        /// </summary>
        [SerializeField] 
        public Transform vrCameraRoot;

        /// <summary>
        /// Reference to the reset position transform
        /// </summary>
        [SerializeField] 
        public Transform resetTransform;
        #endregion

        #region Component References
        [SerializeField] private NetworkTableManager networkTableManager;
        [SerializeField] private CommandManager commandManager;
        [SerializeField] private TelemetryPublisher telemetryPublisher;
        [SerializeField] private QuestNavUI uiManager;
        #endregion

        /// <summary>
        /// Quest display frequency (in Hz)
        /// </summary>
        private float displayFrequency = QuestNavConstants.Thresholds.DEFAULT_DISPLAY_FREQUENCY;
        
        /// <summary>
        /// Counter for updating UI less frequently than per-frame
        /// </summary>
        private int uiUpdateCounter = 0;

        void Start()
        {
            // Set display frequency for Quest
            OVRPlugin.systemDisplayFrequency = displayFrequency;
            
            // Log simulation mode status
            if (QuestNavConstants.USE_SIMULATION_MODE)
            {
                QueuedLogger.Log("[QuestNavCore] Running in SIMULATION MODE");
                QueuedLogger.Log("[QuestNavCore] Using simulation IP: " + QuestNavConstants.SIMULATION_IP_ADDRESS);
            }
            
            // Initialize all subsystems
            InitializeSubsystems();
            
            QueuedLogger.Log("[QuestNavCore] System initialized");
        }

        /// <summary>
        /// Initialize all QuestNav subsystems
        /// </summary>
        private void InitializeSubsystems()
        {
            // Initialize network manager first as other systems depend on it
            networkTableManager.Initialize();
            
            // Initialize remaining subsystems
            commandManager.Initialize(networkTableManager, vrCamera, vrCameraRoot, resetTransform);
            telemetryPublisher.Initialize(networkTableManager, cameraRig);
            uiManager.Initialize(networkTableManager);
        }

        void LateUpdate()
        {
            // Handle Network Connectivity
            if (networkTableManager.IsConnected())
            {
                // Publish telemetry data
                telemetryPublisher.PublishTelemetry();
                
                // Process any incoming commands
                commandManager.ProcessCommands();
            }
            
            // Update UI at a reduced frequency to improve performance
            if (uiUpdateCounter >= QuestNavConstants.Thresholds.UI_UPDATE_INTERVAL)
            {
                uiManager.UpdateStatusDisplay();
                uiUpdateCounter = 0;
            }
            else
            {
                uiUpdateCounter++;
            }
        }

        void OnApplicationQuit()
        {
            // Ensure clean shutdown
            networkTableManager.Disconnect();
            QueuedLogger.Log("[QuestNavCore] System shutdown complete");
        }
    }
}