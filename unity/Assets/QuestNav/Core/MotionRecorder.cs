using System;
using System.IO;
using UnityEngine;

namespace QuestNav.Core
{
    /// <summary>
    /// Records headset motion data to a JSON file for offline analysis
    /// </summary>
    public class MotionRecorder : MonoBehaviour
    {
        [System.Serializable]
        private struct MotionFrame
        {
            public int frameIndex;
            public float timeStamp;
            public Vector3 position;
            public Quaternion rotation;
        }

        /// <summary>
        /// Reference to the OVR Camera Rig for tracking
        /// </summary>
        [SerializeField]
        public OVRCameraRig cameraRig;

        // File writing
        private StreamWriter writer;
        private bool firstFrameWritten = false;
        private string filePath;

        void Start()
        {
            InitializeRecording();
        }

        /// <summary>
        /// Initializes the recording file
        /// </summary>
        private void InitializeRecording()
        {
            Debug.Log("[MotionRecorder] Logging started.");

            filePath = UnityEngine.Application.persistentDataPath + "/motion_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".json";

            try
            {
                writer = new StreamWriter(filePath, false);
                writer.WriteLine("{");
                writer.WriteLine("\"frames\":[");
            }
            catch (IOException e)
            {
                Debug.LogError("[MotionRecorder] ERROR: Failed to create file: " + e.Message);
            }
        }

        void LateUpdate()
        {
            if (writer == null) return;

            try
            {
                // Create frame data
                MotionFrame frame;
                frame.frameIndex = Time.frameCount;
                frame.timeStamp = Time.time;
                frame.position = cameraRig.centerEyeAnchor.position;
                frame.rotation = cameraRig.centerEyeAnchor.rotation;

                // Write to file
                string jsonFrame = JsonUtility.ToJson(frame, true);
                if (firstFrameWritten)
                {
                    writer.WriteLine(",");
                }
                writer.Write(jsonFrame);
                writer.Flush(); // Optional: reduce frequency for performance

                firstFrameWritten = true;
            }
            catch (Exception e)
            {
                Debug.LogError("[MotionRecorder] ERROR: Failed to write frame: " + e.Message);
            }
        }

        void OnApplicationQuit()
        {
            CloseFile();
        }
        
        /// <summary>
        /// Closes the recording file properly
        /// </summary>
        private void CloseFile()
        {
            if (writer != null)
            {
                try
                {
                    writer.WriteLine("]");
                    writer.WriteLine("}");
                    writer.Close();
                    Debug.Log("[MotionRecorder] Logging stopped. File saved to: " + filePath);
                }
                catch (Exception e)
                {
                    Debug.LogError("[MotionRecorder] ERROR: Failed to close file: " + e.Message);
                }
            }
        }
    }
}