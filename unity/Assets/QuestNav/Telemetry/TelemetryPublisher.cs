using UnityEngine;
using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Transformation;

namespace QuestNav.Telemetry
{
    /// <summary>
    /// Handles publishing telemetry data to NetworkTables
    /// </summary>
    public class TelemetryPublisher : MonoBehaviour
    {
        private NetworkTableManager networkTableManager;
        private OVRCameraRig cameraRig;
        
        #region Telemetry Data
        /// <summary>
        /// Current frame index from Unity's Time.frameCount
        /// </summary>
        private int frameIndex;
        
        /// <summary>
        /// Current timestamp from Unity's Time.time
        /// </summary>
        private double timeStamp;
        
        /// <summary>
        /// Current position of the VR headset
        /// </summary>
        private Vector3 position;
        
        /// <summary>
        /// Current rotation of the VR headset as a Quaternion
        /// </summary>
        private Quaternion rotation;
        
        /// <summary>
        /// Current rotation of the VR headset in Euler angles
        /// </summary>
        private Vector3 eulerAngles;
        
        /// <summary>
        /// Current battery percentage of the device
        /// </summary>
        private float batteryPercent;
        #endregion
        
        /// <summary>
        /// Initialize the telemetry publisher
        /// </summary>
        /// <param name="networkManager">Reference to NetworkTableManager</param>
        /// <param name="cameraRig">Reference to OVRCameraRig</param>
        public void Initialize(NetworkTableManager networkManager, OVRCameraRig cameraRig)
        {
            this.networkTableManager = networkManager;
            this.cameraRig = cameraRig;
            QueuedLogger.Log("[TelemetryPublisher] Initialized");
        }
        
        /// <summary>
        /// Publishes telemetry data to NetworkTables
        /// </summary>
        public void PublishTelemetry()
        {
            // Skip if not connected
            if (!networkTableManager.IsConnected()) return;
            
            // Gather current telemetry data
            UpdateTelemetryData();
            
            // Publish all data to NetworkTables
            PublishFrameData();
        }
        
        /// <summary>
        /// Updates telemetry data from current state
        /// </summary>
        private void UpdateTelemetryData()
        {
            frameIndex = Time.frameCount;
            timeStamp = Time.time;
            position = cameraRig.centerEyeAnchor.position;
            rotation = cameraRig.centerEyeAnchor.rotation;
            eulerAngles = cameraRig.centerEyeAnchor.eulerAngles;
            batteryPercent = SystemInfo.batteryLevel * 100;
        }
        
        /// <summary>
        /// Publish frame data to NetworkTables
        /// </summary>
        private void PublishFrameData()
        {
            networkTableManager.PublishValue(QuestNavConstants.Topics.TELEMETRY_FRAME_COUNT, frameIndex);
            networkTableManager.PublishValue(QuestNavConstants.Topics.TELEMETRY_TIMESTAMP, timeStamp);
            networkTableManager.PublishValue(QuestNavConstants.Topics.TELEMETRY_POSITION, position.ToArray());
            networkTableManager.PublishValue(QuestNavConstants.Topics.TELEMETRY_QUATERNION, rotation.ToArray());
            networkTableManager.PublishValue(QuestNavConstants.Topics.TELEMETRY_EULER_ANGLES, eulerAngles.ToArray());
            networkTableManager.PublishValue(QuestNavConstants.Topics.TELEMETRY_BATTERY_PERCENT, batteryPercent);
        }
    }
}