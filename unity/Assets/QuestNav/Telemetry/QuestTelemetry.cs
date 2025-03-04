using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Transformation;
using UnityEngine;

namespace QuestNav.Telemetry
{
    /// <summary>
    /// Collects and publishes telemetry data from the Quest to the robot
    /// </summary>
    public class QuestTelemetry
    {
        // References
        private Transform cameraRig;
        private QuestTransformManager transformManager;
        private QuestNetworkManager networkManager;
    
        // Current frame data
        private int frameIndex;
        private double timeStamp;
        private Vector3 position;
        private Quaternion rotation;
        private Vector3 eulerAngles;
        private float batteryPercent;
    
        /// <summary>
        /// Creates a new telemetry collector
        /// </summary>
        public QuestTelemetry(Transform cameraRig, QuestTransformManager transformManager, QuestNetworkManager networkManager)
        {
            this.cameraRig = cameraRig;
            this.transformManager = transformManager;
            this.networkManager = networkManager;
        }
    
        /// <summary>
        /// Collects current frame data and publishes it to NetworkTables
        /// </summary>
        public bool PublishFrameData()
        {
            try 
            {
                var nt = networkManager.NetworkTables;
                if (nt == null) return false;
            
                // Update frame data
                frameIndex = Time.frameCount;
                timeStamp = Time.time;
            
                // Get raw pose data from camera rig
                Vector3 rawPosition = cameraRig.position;
                Quaternion rawRotation = cameraRig.rotation;
            
                // Apply transform if initialized
                if (transformManager.IsInitialized)
                {
                    // Transform the position and rotation to field space
                    position = transformManager.TransformPosition(rawPosition);
                    rotation = transformManager.TransformRotation(rawRotation);
                    eulerAngles = rotation.eulerAngles;
                }
                else
                {
                    // Use raw values if transform isn't initialized
                    position = rawPosition;
                    rotation = rawRotation;
                    eulerAngles = rawRotation.eulerAngles;
                }
            
                // Get battery information
                batteryPercent = SystemInfo.batteryLevel * 100;

                // Publish values to NetworkTables
                nt.PublishValue(QuestNavConstants.Topics.FRAME_COUNT, frameIndex);
                nt.PublishValue(QuestNavConstants.Topics.TIMESTAMP, timeStamp);
                nt.PublishValue(QuestNavConstants.Topics.POSITION, NetworkExtensions.ToArray(position));
                nt.PublishValue(QuestNavConstants.Topics.QUATERNION, NetworkExtensions.ToArray(rotation));
                nt.PublishValue(QuestNavConstants.Topics.EULER_ANGLES, NetworkExtensions.ToArray(eulerAngles));
                nt.PublishValue(QuestNavConstants.Topics.BATTERY, batteryPercent);
            
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[QuestTelemetry] Error publishing frame data: {ex.Message}");
                return false;
            }
        }
    
        /// <summary>
        /// Gets the current position in field coordinates
        /// </summary>
        public Vector3 GetPosition() => position;
    
        /// <summary>
        /// Gets the current rotation in field coordinates
        /// </summary>
        public Quaternion GetRotation() => rotation;
    
        /// <summary>
        /// Gets the current battery percentage
        /// </summary>
        public float GetBatteryPercent() => batteryPercent;
    }
}