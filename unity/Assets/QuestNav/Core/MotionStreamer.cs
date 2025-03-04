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
            // Check network connection state
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
            }
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