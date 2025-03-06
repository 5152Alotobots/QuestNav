using QuestNav.Commands;
using QuestNav.Network;
using QuestNav.Telemetry;
using QuestNav.Transformation;
using QuestNav.UI;
using UnityEngine;

namespace QuestNav.Core
{
    /// <summary>
    /// Main MonoBehaviour that ties together all Quest navigation components
    /// </summary>
    public class MotionStreamer : MonoBehaviour
    {
        #region Unity References
        /// <summary>
        /// Reference to the OVR Camera Rig for tracking
        /// </summary>
        [SerializeField] 
        private OVRCameraRig cameraRig;
        
        /// <summary>
        /// Reference to the VR camera transform
        /// </summary>
        [SerializeField] 
        private Transform vrCamera;

        /// <summary>
        /// Reference to the VR camera root transform
        /// </summary>
        [SerializeField] 
        private Transform vrCameraRoot;

        /// <summary>
        /// Reference to the reset position transform
        /// </summary>
        [SerializeField] 
        private Transform resetTransform;
        #endregion
        
        #region Component References
        // Core system components
        private QuestNetworkManager networkManager;
        private QuestTransformManager transformManager;
        private QuestUIManager uiManager;
        private QuestTelemetry telemetry;
        private QuestCommandFactory commandFactory;
        private CommandProcessor commandProcessor;
        
        // Command processing delay counter
        private int delayCounter = 0;
        #endregion
        
        /// <summary>
        /// Public access to network manager for other components
        /// </summary>
        public QuestNetworkManager NetworkManager => networkManager;

        #region Unity Lifecycle Methods
        /// <summary>
        /// Initializes the system components
        /// </summary>
        void Start()
        {
            // Set display refresh rate
            OVRPlugin.systemDisplayFrequency = 120.0f;
            
            // Configure Oculus performance settings
            OVRManager.suggestedCpuPerfLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;
            OVRManager.suggestedGpuPerfLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;
            
            // Prevent screen from sleeping
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.runInBackground = true;
            
            // Get UI Manager reference and initialize it
            uiManager = GetComponent<QuestUIManager>();
            uiManager.Initialize();
            uiManager.OnTeamNumberUpdated += HandleTeamNumberUpdated;
            
            // Create system components
            InitializeComponents(uiManager.GetTeamNumber());
        }
        
        /// <summary>
        /// Updates the system state each frame
        /// </summary>
        void LateUpdate()
        {
            // Check network connection state using heartbeat system
            if (networkManager.UpdateConnectionState())
            {
                // Connected, publish telemetry data
                telemetry.PublishFrameData();
                
                // Process commands at a controlled rate
                if (delayCounter >= 0)
                {
                    commandProcessor.ProcessCommands();
                    delayCounter = 0;
                }
                else
                {
                    delayCounter++;
                }
                
                // Update UI based on connection status
                UpdateConnectionUI(networkManager.Status);
            }
            else
            {
                // Not connected, update UI to show disconnected state
                UpdateConnectionUI(QuestNetworkManager.ConnectionStatus.Disconnected);
            }
        }
        
        /// <summary>
        /// Updates the UI based on connection status
        /// </summary>
        private void UpdateConnectionUI(QuestNetworkManager.ConnectionStatus status)
        {
            // This is where you would update any UI elements to show connection status
            // For example, change a status indicator color or text
            switch (status)
            {
                case QuestNetworkManager.ConnectionStatus.Connected:
                    // Green indicator, fully connected
                    break;
                    
                case QuestNetworkManager.ConnectionStatus.Degraded:
                    // Yellow indicator, connection issues
                    break;
                    
                case QuestNetworkManager.ConnectionStatus.Connecting:
                    // Blue indicator, trying to establish connection
                    break;
                    
                case QuestNetworkManager.ConnectionStatus.Disconnected:
                    // Red indicator, no connection
                    break;
            }
        }
        
        /// <summary>
        /// Handles app pause and resume events, including standby
        /// </summary>
        /// <param name="pauseStatus">True if pausing, false if resuming</param>
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App is pausing (going to standby)
                Debug.Log("[MotionStreamer] Application paused, preparing for standby");
                if (networkManager != null)
                {
                    networkManager.HandleStandbyEnter();
                }
            }
            else
            {
                // App is resuming from pause (waking from standby)
                Debug.Log("[MotionStreamer] Application resumed from standby");
                // Ensure screen won't sleep
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                
                if (networkManager != null)
                {
                    networkManager.HandleStandbyExit();
                }
            }
        }
        
        /// <summary>
        /// Handles focus changes in the application
        /// </summary>
        /// <param name="hasFocus">Whether the application has focus</param>
        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                // App gained focus
                Debug.Log("[MotionStreamer] Application focus regained");
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                
                if (networkManager != null)
                {
                    // Similar to standby exit, consider reconnecting if needed
                    networkManager.HandleStandbyExit();
                }
            }
            else
            {
                // App lost focus
                Debug.Log("[MotionStreamer] Application focus lost");
                
                if (networkManager != null)
                {
                    // Similar to standby enter, prepare for potential network issues
                    networkManager.HandleStandbyEnter();
                }
            }
        }
        
        /// <summary>
        /// Called when the component is enabled
        /// </summary>
        void OnEnable()
        {
            // Ensure these settings are applied whenever the component is enabled
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.runInBackground = true;
        }
        
        /// <summary>
        /// Clean up on application exit
        /// </summary>
        void OnApplicationQuit()
        {
            // Send a disconnect notification to the robot
            if (networkManager != null && networkManager.NetworkTables != null)
            {
                try
                {
                    // Use the proper disconnect response code
                    networkManager.NetworkTables.PublishValue(QuestNavConstants.Topics.MISO, QuestNavConstants.RESP_DISCONNECT);
                    Debug.Log("[MotionStreamer] Sent shutdown notification to robot");
                    
                    // Allow some time for the message to be sent before disconnecting
                    System.Threading.Thread.Sleep(100);
                    
                    // Properly disconnect
                    networkManager.Disconnect();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[MotionStreamer] Error sending shutdown notification: {ex.Message}");
                }
            }
            
            Debug.Log("[MotionStreamer] Application shutting down");
        }
        #endregion
        
        #region Initialization Methods
        /// <summary>
        /// Initializes all system components
        /// </summary>
        /// <param name="teamNumber">FRC team number</param>
        private void InitializeComponents(string teamNumber)
        {
            // Create network manager
            networkManager = new QuestNetworkManager(teamNumber);
            
            // Create transform manager
            transformManager = new QuestTransformManager(vrCamera, vrCameraRoot, resetTransform);
            
            // Create telemetry
            telemetry = new QuestTelemetry(cameraRig.centerEyeAnchor, transformManager, networkManager);
            
            // Create command factory
            commandFactory = new QuestCommandFactory(transformManager, networkManager);
            
            // Create command processor
            commandProcessor = new CommandProcessor(networkManager, commandFactory);
        }
        
        /// <summary>
        /// Handles team number updates from the UI
        /// </summary>
        /// <param name="newTeamNumber">New team number</param>
        private void HandleTeamNumberUpdated(string newTeamNumber)
        {
            networkManager.UpdateTeamNumber(newTeamNumber);
        }
        #endregion
    }
}