using UnityEngine;

namespace QuestNav.Transformation
{
    /// <summary>
    /// Handles coordinate conversion between FRC and Unity coordinate systems
    /// </summary>
    public static class CoordinateConverter
    {
        /// <summary>
        /// Converts FRC field coordinates to Unity world coordinates
        /// 
        /// FRC coordinates:
        /// - X is along the length of the field (forward)
        /// - Y is along the width of the field (left)
        /// - Z points up with gravity (up)
        /// - CCW positive rotation around Z
        /// 
        /// Unity coordinates:
        /// - X is right
        /// - Y is up
        /// - Z is forward 
        /// - CW positive rotation around Y
        /// </summary>
        /// <param name="frcX">FRC X coordinate (along length)</param>
        /// <param name="frcY">FRC Y coordinate (along width)</param>
        /// <param name="frcRotation">FRC rotation in degrees (CCW positive)</param>
        /// <param name="currentHeight">Current height in Unity to maintain</param>
        /// <returns>Unity position vector</returns>
        public static Vector3 ConvertFRCToUnityPosition(float frcX, float frcY, float currentHeight)
        {
            // Convert FRC coordinates to Unity:
            // - Unity X = -FRC Y (right is positive in Unity)
            // - Unity Y = kept unchanged (height)
            // - Unity Z = FRC X (forward is positive in both)
            return new Vector3(-frcY, currentHeight, frcX);
        }

        /// <summary>
        /// Converts FRC rotation to Unity rotation
        /// </summary>
        /// <param name="frcRotation">FRC rotation in degrees (CCW positive)</param>
        /// <returns>Unity rotation angle in degrees (CW positive)</returns>
        public static float ConvertFRCToUnityRotation(float frcRotation)
        {
            // Unity uses clockwise rotation (negative to FRC)
            // Negate the angle and normalize to 0-360 range
            float unityRotation = -frcRotation;
            while (unityRotation < 0) unityRotation += 360;
            while (unityRotation >= 360) unityRotation -= 360;

            return unityRotation;
        }

        /// <summary>
        /// Converts Unity position to FRC field coordinates
        /// </summary>
        /// <param name="unityPosition">Position in Unity world space</param>
        /// <returns>FRC coordinates [X, Y]</returns>
        public static Vector2 ConvertUnityToFRCPosition(Vector3 unityPosition)
        {
            // Convert Unity coordinates to FRC:
            // - FRC X = Unity Z
            // - FRC Y = -Unity X
            return new Vector2(unityPosition.z, -unityPosition.x);
        }

        /// <summary>
        /// Converts Unity rotation to FRC rotation
        /// </summary>
        /// <param name="unityRotation">Unity Y-axis rotation in degrees</param>
        /// <returns>FRC rotation in degrees (CCW positive)</returns>
        public static float ConvertUnityToFRCRotation(float unityRotation)
        {
            // Negate the angle to convert from CW to CCW
            // Normalize to -180 to 180 range as commonly used in FRC
            float frcRotation = -unityRotation;
            while (frcRotation <= -180) frcRotation += 360;
            while (frcRotation > 180) frcRotation -= 360;

            return frcRotation;
        }

        /// <summary>
        /// Normalizes an angle to the -180 to 180 degree range
        /// </summary>
        /// <param name="angle">Angle in degrees</param>
        /// <returns>Normalized angle in the -180 to 180 range</returns>
        public static float NormalizeAngle180(float angle)
        {
            while (angle <= -180) angle += 360;
            while (angle > 180) angle -= 360;
            return angle;
        }

        /// <summary>
        /// Normalizes an angle to the 0 to 360 degree range
        /// </summary>
        /// <param name="angle">Angle in degrees</param>
        /// <returns>Normalized angle in the 0 to 360 range</returns>
        public static float NormalizeAngle360(float angle)
        {
            while (angle < 0) angle += 360;
            while (angle >= 360) angle -= 360;
            return angle;
        }
    }

    /// <summary>
    /// Extension methods for Unity's Vector3 class to convert to array format
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// Converts a Vector3 to a float array containing x, y, and z components
        /// </summary>
        /// <param name="vector">The Vector3 to convert</param>
        /// <returns>Float array containing [x, y, z] values</returns>
        public static float[] ToArray(this Vector3 vector)
        {
            return new float[] { vector.x, vector.y, vector.z };
        }
    }

    /// <summary>
    /// Extension methods for Unity's Quaternion class to convert to array format
    /// </summary>
    public static class QuaternionExtensions
    {
        /// <summary>
        /// Converts a Quaternion to a float array containing x, y, z, and w components
        /// </summary>
        /// <param name="quaternion">The Quaternion to convert</param>
        /// <returns>Float array containing [x, y, z, w] values</returns>
        public static float[] ToArray(this Quaternion quaternion)
        {
            return new float[] { quaternion.x, quaternion.y, quaternion.z, quaternion.w };
        }
    }
}