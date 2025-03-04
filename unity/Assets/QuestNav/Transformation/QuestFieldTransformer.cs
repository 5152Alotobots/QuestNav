using UnityEngine;

namespace QuestNav.Transformation
{
    /// <summary>
    /// Utility class for transforming between Quest VR space and FRC field coordinates
    /// </summary>
    public static class QuestFieldTransformer
    {

        /// <summary>
        /// Converts FRC field coordinates to Unity world coordinates
        /// </summary>
        /// <param name="frcX">FRC X position (along field length)</param>
        /// <param name="frcY">FRC Y position (along field width)</param>
        /// <param name="frcRotationDegrees">FRC rotation in degrees (CCW positive)</param>
        /// <param name="heightPreserve">Optional Unity Y value to preserve (defaults to 0)</param>
        /// <returns>Unity position and rotation as a tuple</returns>
        public static (Vector3 position, Quaternion rotation) FrcToUnity(
            float frcX, 
            float frcY, 
            float frcRotationDegrees,
            float heightPreserve = 0f)
        {
            // Convert FRC coordinates to Unity coordinates:
            // - Unity X = -FRC Y (right is positive in Unity)
            // - Unity Y = kept as provided height parameter
            // - Unity Z = FRC X (forward is positive in both)
            Vector3 unityPosition = new Vector3(
                -frcY,
                heightPreserve,
                frcX
            );

            // Convert FRC rotation to Unity rotation:
            // - Unity uses clockwise positive rotation around Y
            // - FRC uses counterclockwise positive rotation
            float unityYawDegrees = -frcRotationDegrees;  // Negate for Unity's rotation direction
        
            // Create quaternion from Euler angles (only Y rotation matters for top-down view)
            Quaternion unityRotation = Quaternion.Euler(0, unityYawDegrees, 0);

            return (unityPosition, unityRotation);
        }

        /// <summary>
        /// Converts Unity world coordinates to FRC field coordinates
        /// </summary>
        /// <param name="unityPosition">Unity position vector</param>
        /// <param name="unityRotation">Unity rotation quaternion</param>
        /// <returns>FRC X, Y and rotation in degrees as a tuple</returns>
        public static (float frcX, float frcY, float frcRotationDegrees) UnityToFrc(
            Vector3 unityPosition,
            Quaternion unityRotation)
        {
            // Convert Unity coordinates to FRC coordinates:
            // - FRC X = Unity Z
            // - FRC Y = -Unity X
            float frcX = unityPosition.z;
            float frcY = -unityPosition.x;

            // Get Unity rotation in Euler angles
            float unityYawDegrees = unityRotation.eulerAngles.y;
        
            // Normalize to 0-360 range
            while (unityYawDegrees < 0) unityYawDegrees += 360;
            while (unityYawDegrees >= 360) unityYawDegrees -= 360;

            // Convert to FRC rotation (CCW positive)
            float frcRotationDegrees = -unityYawDegrees;
        
            // Normalize to -180 to 180 range
            while (frcRotationDegrees > 180) frcRotationDegrees -= 360;
            while (frcRotationDegrees <= -180) frcRotationDegrees += 360;

            return (frcX, frcY, frcRotationDegrees);
        }

        /// <summary>
        /// Calculates the transformation matrix to convert from Quest's local space to FRC field space
        /// </summary>
        /// <param name="questLocalPosition">Current Quest position in local space</param>
        /// <param name="questLocalRotation">Current Quest rotation in local space</param>
        /// <param name="frcX">Target FRC X position</param>
        /// <param name="frcY">Target FRC Y position</param>
        /// <param name="frcRotationDegrees">Target FRC rotation in degrees</param>
        /// <returns>The transformation matrix</returns>
        public static Matrix4x4 CalculateQuestToFieldTransform(
            Vector3 questLocalPosition,
            Quaternion questLocalRotation,
            float frcX,
            float frcY,
            float frcRotationDegrees)
        {
            // Convert FRC position to Unity position
            (Vector3 unityFieldPosition, Quaternion unityFieldRotation) = 
                FrcToUnity(frcX, frcY, frcRotationDegrees, questLocalPosition.y);
        
            // Create matrices
            Matrix4x4 questLocalMatrix = Matrix4x4.TRS(questLocalPosition, questLocalRotation, Vector3.one);
            Matrix4x4 fieldMatrix = Matrix4x4.TRS(unityFieldPosition, unityFieldRotation, Vector3.one);
        
            // Calculate transform: questToField = fieldMatrix * questLocalMatrix.inverse
            return fieldMatrix * questLocalMatrix.inverse;
        }

        /// <summary>
        /// Applies a transformation matrix to a position
        /// </summary>
        /// <param name="transformMatrix">The transformation matrix</param>
        /// <param name="position">The position to transform</param>
        /// <returns>The transformed position</returns>
        public static Vector3 TransformPosition(Matrix4x4 transformMatrix, Vector3 position)
        {
            return transformMatrix.MultiplyPoint3x4(position);
        }

        /// <summary>
        /// Applies a transformation matrix to a rotation
        /// </summary>
        /// <param name="transformMatrix">The transformation matrix</param>
        /// <param name="rotation">The rotation to transform</param>
        /// <returns>The transformed rotation</returns>
        public static Quaternion TransformRotation(Matrix4x4 transformMatrix, Quaternion rotation)
        {
            // Extract the rotation from the transformation matrix
            // This works because we're not using scaling
            return Quaternion.LookRotation(
                transformMatrix.GetColumn(2),  // Forward direction
                transformMatrix.GetColumn(1)   // Up direction
            ) * rotation;
        }

        /// <summary>
        /// Validates if FRC coordinates are within field boundaries
        /// </summary>
        /// <param name="frcX">FRC X position (along field length)</param>
        /// <param name="frcY">FRC Y position (along field width)</param>
        /// <returns>True if coordinates are valid, false otherwise</returns>
        public static bool ValidateFrcCoordinates(float frcX, float frcY)
        {
            return frcX is >= 0 and <= Core.QuestNavConstants.FIELD_LENGTH && 
                   frcY is >= 0 and <= Core.QuestNavConstants.FIELD_WIDTH;
        }
    }
}