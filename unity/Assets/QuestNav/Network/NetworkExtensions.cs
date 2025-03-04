using UnityEngine;

namespace QuestNav.Network
{
    /// <summary>
    /// Extension methods for Unity types to facilitate NetworkTables communication
    /// </summary>
    public static class NetworkExtensions
    {
        /// <summary>
        /// Converts a Vector3 to a float array containing x, y, and z components.
        /// </summary>
        /// <param name="vector">The Vector3 to convert</param>
        /// <returns>Float array containing [x, y, z] values</returns>
        public static float[] ToArray(this Vector3 vector)
        {
            return new float[] { vector.x, vector.y, vector.z };
        }
    
        /// <summary>
        /// Converts a Quaternion to a float array containing x, y, z, and w components.
        /// </summary>
        /// <param name="quaternion">The Quaternion to convert</param>
        /// <returns>Float array containing [x, y, z, w] values</returns>
        public static float[] ToArray(this Quaternion quaternion)
        {
            return new float[] { quaternion.x, quaternion.y, quaternion.z, quaternion.w };
        }
    }
}