using System;
using QuestNav.Core;
using QuestNav.Network.NetworkTables_CSharp;
using UnityEngine;

namespace QuestNav.Transformation
{
    /// <summary>
    /// Manages all pose transformations, resets, and spatial operations for the Quest
    /// </summary>
    public class QuestTransformManager
    {
        // Transform matrix for Quest to Field conversion
        private Matrix4x4 questToFieldTransform = Matrix4x4.identity;
        private bool transformInitialized = false;

        // References to Unity objects
        private Transform vrCamera;
        private Transform vrCameraRoot;
        private Transform resetTransform;
    
        /// <summary>
        /// Creates a new transform manager with references to the necessary Unity transforms
        /// </summary>
        public QuestTransformManager(Transform vrCamera, Transform vrCameraRoot, Transform resetTransform)
        {
            this.vrCamera = vrCamera;
            this.vrCameraRoot = vrCameraRoot;
            this.resetTransform = resetTransform;
        }
    
        /// <summary>
        /// Whether the transform matrix has been initialized
        /// </summary>
        public bool IsInitialized => transformInitialized;
    
        /// <summary>
        /// Transforms a raw position from Quest space to field space
        /// </summary>
        public Vector3 TransformPosition(Vector3 rawPosition)
        {
            if (!transformInitialized)
            {
                return rawPosition;
            }
            return QuestFieldTransformer.TransformPosition(questToFieldTransform, rawPosition);
        }
    
        /// <summary>
        /// Transforms a raw rotation from Quest space to field space
        /// </summary>
        public Quaternion TransformRotation(Quaternion rawRotation)
        {
            if (!transformInitialized)
            {
                return rawRotation;
            }
            return QuestFieldTransformer.TransformRotation(questToFieldTransform, rawRotation);
        }
    
        /// <summary>
        /// Updates the transformation matrix based on FRC coordinates
        /// </summary>
        public bool UpdateTransformFromFrcCoordinates(float frcX, float frcY, float frcRotation)
        {
            // Validate coordinates
            if (!QuestFieldTransformer.ValidateFrcCoordinates(frcX, frcY))
            {
                Debug.LogWarning($"[QuestTransformManager] Invalid FRC coordinates: X:{frcX} Y:{frcY}");
                return false;
            }
        
            // Convert to Unity coordinates
            var (fieldPosition, fieldRotation) = QuestFieldTransformer.FrcToUnity(
                frcX, frcY, frcRotation, vrCamera.position.y);
        
            // Update the transform matrix
            UpdateTransformMatrix(fieldPosition, fieldRotation);
            return true;
        }
    
        /// <summary>
        /// Updates the transformation matrix directly with Unity coordinates
        /// </summary>
        public void UpdateTransformMatrix(Vector3 fieldPosition, Quaternion fieldRotation)
        {
            try 
            {
                // Current Quest pose in its local space
                Vector3 questPosition = vrCamera.position;
                Quaternion questRotation = vrCamera.rotation;
            
                // Create matrices
                Matrix4x4 questPose = Matrix4x4.TRS(questPosition, questRotation, Vector3.one);
                Matrix4x4 fieldPose = Matrix4x4.TRS(fieldPosition, fieldRotation, Vector3.one);
            
                // Calculate the transformation: questToField = fieldPose * questPose.inverse
                questToFieldTransform = fieldPose * questPose.inverse;
                transformInitialized = true;
            
                // Log for debugging
                Debug.Log($"[QuestTransformManager] Transform updated - Field: {fieldPosition}, Quest: {questPosition}");
            }
            catch (Exception e) 
            {
                Debug.LogError($"[QuestTransformManager] Error updating transform: {e.Message}");
                Debug.LogException(e);
            }
        }
    
        /// <summary>
        /// Processes a pose reset command from the robot
        /// </summary>
        public bool ProcessPoseReset(Nt4Source networkTables)
        {
            try 
            {
                // Constants for retry logic
                const int maxRetries = QuestNavConstants.MAX_POSE_RESET_RETRIES;
                const float retryDelayMs = QuestNavConstants.POSE_RESET_RETRY_DELAY;

                double[] resetPose = null;
                bool success = false;
                int attemptCount = 0;

                // Attempt to read pose data from NetworkTables with retry logic
                for (int i = 0; i < maxRetries && !success; i++)
                {
                    attemptCount++;
                    // Add delay between retries to allow for network latency
                    if (i > 0) 
                    {
                        System.Threading.Thread.Sleep((int)retryDelayMs);
                        Debug.Log($"[QuestTransformManager] Attempt {attemptCount} of {maxRetries}...");
                    }

                    // Read the pose array from NetworkTables
                    // Format: [X, Y, Rotation] in FRC field coordinates
                    resetPose = networkTables.GetDoubleArray(QuestNavConstants.Topics.RESET_POSE);

                    // Validate pose data format and field boundaries
                    if (resetPose != null && resetPose.Length == 3) 
                    {
                        // Check if pose is within valid field boundaries
                        float frcX = (float)resetPose[0];
                        float frcY = (float)resetPose[1];
                    
                        if (!QuestFieldTransformer.ValidateFrcCoordinates(frcX, frcY))
                        {
                            Debug.LogWarning($"[QuestTransformManager] Reset pose outside field boundaries: X:{resetPose[0]:F3} Y:{resetPose[1]:F3}");
                            continue;
                        }
                        success = true;
                        Debug.Log($"[QuestTransformManager] Successfully read reset pose values on attempt {attemptCount}");
                    }

                    Debug.Log($"[QuestTransformManager] Values (Attempt {attemptCount}): X:{resetPose?[0]:F3} Y:{resetPose?[1]:F3} Rot:{resetPose?[2]:F3}");
                }

                // Exit if we couldn't get valid pose data
                if (!success) 
                {
                    Debug.LogWarning($"[QuestTransformManager] Failed to read valid reset pose values after {attemptCount} attempts");
                    return false;
                }

                // Extract pose components from the array
                float resetX = (float)resetPose[0];        // FRC X coordinate (along length of field)
                float resetY = (float)resetPose[1];        // FRC Y coordinate (along width of field)
                float resetRotation = (float)resetPose[2]; // FRC rotation (CCW positive)

                // Normalize rotation to -180 to 180 degrees range
                while (resetRotation > 180) resetRotation -= 360;
                while (resetRotation < -180) resetRotation += 360;

                Debug.Log($"[QuestTransformManager] Starting pose reset - Target: FRC X:{resetX:F2} Y:{resetY:F2} Rot:{resetRotation:F2}°");

                // Store current VR camera state for reference
                Vector3 currentCameraPos = vrCamera.position;
                Quaternion currentCameraRot = vrCamera.rotation;

                Debug.Log($"[QuestTransformManager] Before reset - Camera Pos:{currentCameraPos:F3} Rot:{currentCameraRot.eulerAngles:F3}");

                // Use our utility class to handle the conversion
                var (targetUnityPosition, targetUnityRotation) = QuestFieldTransformer.FrcToUnity(
                    resetX, resetY, resetRotation, currentCameraPos.y);

                // Calculate the position difference we need to move
                Vector3 positionDelta = targetUnityPosition - currentCameraPos;

                // Calculate rotation difference
                float currentYaw = currentCameraRot.eulerAngles.y;
                float targetYaw = targetUnityRotation.eulerAngles.y;
            
                // Normalize both angles to 0-360 range for proper delta calculation
                while (currentYaw < 0) currentYaw += 360;
                while (targetYaw < 0) targetYaw += 360;

                // Calculate rotation delta and normalize to -180 to 180 range
                float yawDelta = targetYaw - currentYaw;
                if (yawDelta > 180) yawDelta -= 360;
                if (yawDelta < -180) yawDelta += 360;

                Debug.Log($"[QuestTransformManager] Calculated adjustments - Position delta:{positionDelta:F3} Rotation delta:{yawDelta:F3}°");
                Debug.Log($"[QuestTransformManager] Target Unity Position: {targetUnityPosition:F3}");

                // First rotate the root around the camera position
                // This maintains the camera's position while changing orientation
                vrCameraRoot.RotateAround(vrCamera.position, Vector3.up, yawDelta);

                // Then apply the position adjustment to achieve target position
                vrCameraRoot.position += positionDelta;

                // Also initialize our transform matrix for future updates
                UpdateTransformMatrix(targetUnityPosition, targetUnityRotation);

                // Log final position and rotation for verification
                Debug.Log($"[QuestTransformManager] After reset - Camera Pos:{vrCamera.position:F3} Rot:{vrCamera.rotation.eulerAngles:F3}");

                // Calculate and check position error to ensure accuracy
                float posError = Vector3.Distance(vrCamera.position, targetUnityPosition);
                Debug.Log($"[QuestTransformManager] Position error after reset: {posError:F3}m");

                // Warn if position error is larger than expected threshold
                if (posError > QuestNavConstants.POSITION_ERROR_THRESHOLD)
                {
                    Debug.LogWarning($"[QuestTransformManager] Large position error detected: {posError:F3}m");
                }

                return true;
            }
            catch (Exception e) 
            {
                Debug.LogError($"[QuestTransformManager] Error during pose reset: {e.Message}");
                Debug.LogException(e);
                return false;
            }
        }
    
        /// <summary>
        /// Recenters the player's view (heading reset)
        /// </summary>
        public bool RecenterPlayer()
        {
            try 
            {
                // Calculate the rotation angle difference between current camera and reset position
                float rotationAngleY = vrCamera.rotation.eulerAngles.y - resetTransform.rotation.eulerAngles.y;

                // Apply negative rotation to camera root to align with reset orientation
                vrCameraRoot.Rotate(0, -rotationAngleY, 0);

                // Calculate position difference and apply to camera root
                Vector3 distanceDiff = resetTransform.position - vrCamera.position;
                vrCameraRoot.position += distanceDiff;

                return true;
            }
            catch (Exception e) 
            {
                Debug.LogError($"[QuestTransformManager] Error during player recenter: {e.Message}");
                Debug.LogException(e);
                return false;
            }
        }
    }
}