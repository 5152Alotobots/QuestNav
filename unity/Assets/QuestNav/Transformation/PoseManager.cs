using UnityEngine;
using QuestNav.Telemetry;
using QuestNav.Core;

namespace QuestNav.Transformation
{
    /// <summary>
    /// Manages pose transformations and resets
    /// </summary>
    public class PoseManager : MonoBehaviour
    {
        private Transform vrCamera;
        private Transform vrCameraRoot;
        private Transform resetTransform;
        
        /// <summary>
        /// Initialize the pose manager with required references
        /// </summary>
        public void Initialize(Transform vrCamera, Transform vrCameraRoot, Transform resetTransform)
        {
            this.vrCamera = vrCamera;
            this.vrCameraRoot = vrCameraRoot;
            this.resetTransform = resetTransform;
            QueuedLogger.Log("[PoseManager] Initialized");
        }
        
        /// <summary>
        /// Resets the pose based on FRC field coordinates
        /// </summary>
        /// <param name="resetX">FRC X coordinate (along field length)</param>
        /// <param name="resetY">FRC Y coordinate (along field width)</param>
        /// <param name="resetRotation">FRC rotation in degrees (CCW positive)</param>
        /// <returns>True if reset was successful, false otherwise</returns>
        public bool ResetPose(float resetX, float resetY, float resetRotation)
        {
            try
            {
                QueuedLogger.Log($"[PoseManager] Starting pose reset - Target: FRC X:{resetX:F2} Y:{resetY:F2} Rot:{resetRotation:F2}°");
                
                // Store current VR camera state for reference
                Vector3 currentCameraPos = vrCamera.position;
                Quaternion currentCameraRot = vrCamera.rotation;
                
                QueuedLogger.Log($"[PoseManager] Before reset - Camera Pos:{currentCameraPos:F3} Rot:{currentCameraRot.eulerAngles:F3}");
                
                // Convert FRC coordinates to Unity coordinates
                Vector3 targetUnityPosition = CoordinateConverter.ConvertFRCToUnityPosition(
                    resetX, resetY, currentCameraPos.y);
                
                // Calculate the position difference we need to move
                Vector3 positionDelta = targetUnityPosition - currentCameraPos;
                
                // Convert FRC rotation to Unity rotation
                float targetUnityYaw = CoordinateConverter.ConvertFRCToUnityRotation(resetRotation);
                float currentYaw = currentCameraRot.eulerAngles.y;
                
                // Calculate rotation delta and normalize to -180 to 180 range
                float yawDelta = targetUnityYaw - currentYaw;
                yawDelta = CoordinateConverter.NormalizeAngle180(yawDelta);
                
                QueuedLogger.Log($"[PoseManager] Calculated adjustments - Position delta:{positionDelta:F3} Rotation delta:{yawDelta:F3}°");
                QueuedLogger.Log($"[PoseManager] Target Unity Position: {targetUnityPosition:F3}");
                
                // First rotate the root around the camera position
                // This maintains the camera's position while changing orientation
                vrCameraRoot.RotateAround(vrCamera.position, Vector3.up, yawDelta);
                
                // Then apply the position adjustment to achieve target position
                vrCameraRoot.position += positionDelta;
                
                // Log final position and rotation for verification
                QueuedLogger.Log($"[PoseManager] After reset - Camera Pos:{vrCamera.position:F3} Rot:{vrCamera.rotation.eulerAngles:F3}");
                
                // Calculate and check position error to ensure accuracy
                float posError = Vector3.Distance(vrCamera.position, targetUnityPosition);
                QueuedLogger.Log($"[PoseManager] Position error after reset: {posError:F3}m");
                
                // Warn if position error is larger than expected threshold
                if (posError > QuestNavConstants.Thresholds.POSITION_ERROR_THRESHOLD)
                {
                    QueuedLogger.LogWarning($"[PoseManager] Large position error detected!");
                }
                
                return true;
            }
            catch (System.Exception e)
            {
                QueuedLogger.LogError($"[PoseManager] Error during pose reset: {e.Message}");
                QueuedLogger.LogException(e);
                return false;
            }
        }
        
        /// <summary>
        /// Recenters the player to face the forward direction
        /// Similar to the effect of long-pressing the Oculus button
        /// </summary>
        /// <returns>True if recenter was successful, false otherwise</returns>
        public bool RecenterHeading()
        {
            try
            {
                QueuedLogger.Log("[PoseManager] Recentering player heading");
                
                // Calculate the rotation angle needed
                float rotationAngleY = vrCamera.rotation.eulerAngles.y - resetTransform.rotation.eulerAngles.y;
                
                // Apply the rotation adjustment to the camera root
                vrCameraRoot.transform.Rotate(0, -rotationAngleY, 0);
                
                // Apply position adjustment to reset position
                Vector3 distanceDiff = resetTransform.position - vrCamera.position;
                vrCameraRoot.transform.position += distanceDiff;
                
                QueuedLogger.Log("[PoseManager] Recenter complete");
                return true;
            }
            catch (System.Exception e)
            {
                QueuedLogger.LogError($"[PoseManager] Error during heading reset: {e.Message}");
                QueuedLogger.LogException(e);
                return false;
            }
        }
    }
}