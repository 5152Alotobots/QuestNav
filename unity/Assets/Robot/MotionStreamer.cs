using UnityEngine;
using NetworkTables;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using TMPro;
using UnityEngine.UI;
using System;

/// <summary>
/// Extension methods for Unity's Vector3 class to convert to array format.
/// </summary>
public static class VectorExtensions
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
}

/// <summary>
/// Extension methods for Unity's Quaternion class to convert to array format.
/// </summary>
public static class QuaternionExtensions
{
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

/// <summary>
/// Manages streaming of VR motion data to a FRC robot through NetworkTables.
/// Handles pose tracking, reset operations, and network communication.
/// </summary>
public class MotionStreamer : MonoBehaviour
{
    #region Fields
    /// <summary>
    /// Current frame index from Unity's Time.frameCount
    /// </summary>
    public int frameIndex;

    /// <summary>
    /// Current timestamp from Unity's Time.time
    /// </summary>
    public double timeStamp;

    /// <summary>
    /// Current position of the VR headset
    /// </summary>
    public Vector3 position;

    /// <summary>
    /// Current rotation of the VR headset as a Quaternion
    /// </summary>
    public Quaternion rotation;

    /// <summary>
    /// Current rotation of the VR headset in Euler angles
    /// </summary>
    public Vector3 eulerAngles;

    /// <summary>
    /// Reference to the OVR Camera Rig for tracking
    /// </summary>
    public OVRCameraRig cameraRig;

    /// <summary>
    /// NetworkTables connection for FRC data communication
    /// </summary>
    public Nt4Source frcDataSink = null;

    /// <summary>
    /// Current command received from the robot
    /// </summary>
    private long command = 0;

    /// <summary>
    /// Flag indicating if a reset operation is in progress
    /// </summary>
    private bool resetInProgress = false;

    /// <summary>
    /// Input field for team number entry
    /// </summary>
    public TMP_InputField teamInput;

    /// <summary>
    /// Button to update team number
    /// </summary>
    public Button teamUpdateButton;

    /// <summary>
    /// Default team number text
    /// </summary>
    public static string inputText = "5152";

    /// <summary>
    /// Current team number
    /// </summary>
    private string teamNumber = "";

    /// <summary>
    /// Reference to the VR camera transform
    /// </summary>
    [SerializeField] 
    public Transform vrCamera;

    /// <summary>
    /// Reference to the VR camera root transform
    /// </summary>
    [SerializeField] 
    public Transform vrCameraRoot;

    /// <summary>
    /// Reference to the reset position transform
    /// </summary>
    [SerializeField] 
    public Transform resetTransform;

    /// <summary>
    /// Current battery percentage of the device
    /// </summary>
    private float batteryPercent = 0;

    // Reconnection fields
    private bool isReconnecting = false;
    private float reconnectDelay = 0.25f; // Short fixed delay between attempts
    private float reconnectTimer = 0f;
    private int reconnectAttempts = 0;
    private bool topicsPublished = false;

    // Connection health monitoring
    private float lastReceiveTime = 0f;
    private const float DATA_TIMEOUT = 5.0f; // 5 seconds without receiving data = stale connection

    #region NetworkTables Configuration
    /// <summary>
    /// Application name for NetworkTables connection
    /// </summary>
    private readonly string appName = "Quest3S";

    /// <summary>
    /// Server address format for NetworkTables connection
    /// </summary>
    private readonly string serverAddress = "10.TE.AM.2";

    /// <summary>
    /// DNS format for roboRIO connection
    /// </summary>
    private readonly string serverDNS = "roboRIO-####-FRC.local";

    /// <summary>
    /// Current IP address for connection
    /// </summary>
    private string ipAddress = "";

    /// <summary>
    /// Flag to toggle between IP and DNS connection methods
    /// </summary>
    private bool useAddress = true;

    /// <summary>
    /// NetworkTables server port
    /// </summary>
    private readonly int serverPort = 5810;

    /// <summary>
    /// Counter for connection retry delay
    /// </summary>
    private int delayCounter = 0;
    #endregion
    #endregion

    #region Unity Lifecycle Methods
    /// <summary>
    /// Initializes the connection and UI components
    /// </summary>
    void Start()
    {
        OVRPlugin.systemDisplayFrequency = 120.0f;
        teamNumber = PlayerPrefs.GetString("TeamNumber", "5152");
        setInputBox(teamNumber);
        teamInput.Select();
        ConnectToRobot();
        teamUpdateButton.onClick.AddListener(UpdateTeamNumber);
        teamInput.onSelect.AddListener(OnInputFieldSelected);
        lastReceiveTime = Time.time; // Initialize last receive time
    }

    /// <summary>
    /// Handles frame updates for data publishing and command processing
    /// </summary>
    void LateUpdate()
    {
        // Check for stale connections and force reconnect if needed
        if (frcDataSink != null && frcDataSink.Client != null && frcDataSink.Client.Connected())
        {
            // Reset reconnection state if we're connected
            if (isReconnecting)
            {
                isReconnecting = false;
                Debug.Log("[MotionStreamer] Connection restored");
            }
            
            // Force reconnection if we haven't received data in a while
            if (Time.time - lastReceiveTime > DATA_TIMEOUT) {
                Debug.Log("[MotionStreamer] Connection appears stale. Forcing reconnection...");
                HandleDisconnectedState();
                return;
            }

            // Ensure topics are properly published
            if (!topicsPublished) {
                PublishTopics();
            }
            
            // Normal connected operation
            PublishFrameData();

            if (delayCounter >= 0)
            {
                ProcessCommands();
                delayCounter = 0;
            }
            else
            {
                delayCounter++;
            }
        }
        else
        {
            // Handle reconnection if not connected
            topicsPublished = false;
            HandleDisconnectedState();
        }
    }
    #endregion

    #region Network Connection Methods
    /// <summary>
    /// Establishes connection to the robot using either IP or DNS
    /// </summary>
    private void ConnectToRobot()
    {
        string attemptedAddress = "Unknown";
        
        try
        {
            if (useAddress == true)
            {
                ipAddress = getIP();
                attemptedAddress = ipAddress;
                useAddress = false;
            }
            else
            {
                try
                {
                    string dnsName = getDNS();
                    attemptedAddress = dnsName;
                    IPHostEntry hostEntry = Dns.GetHostEntry(dnsName);
                    IPAddress[] ipv4Addresses = hostEntry.AddressList
                       .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                       .ToArray();
                       
                    if (ipv4Addresses.Length > 0)
                    {
                        ipAddress = ipv4Addresses[0].ToString();
                    }
                    else
                    {
                        Debug.LogWarning($"[MotionStreamer] No IPv4 addresses found for {dnsName}, falling back to IP-based connection");
                        ipAddress = getIP();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MotionStreamer] Error resolving DNS name: {ex.Message}, falling back to IP-based connection");
                    ipAddress = getIP();
                }

                useAddress = true;
            }
            
            Debug.Log($"[MotionStreamer] Attempting to connect to the RoboRIO at {ipAddress} (via {attemptedAddress}).");
            frcDataSink = new Nt4Source(appName, ipAddress, serverPort);
            PublishTopics();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MotionStreamer] Failed to connect to {attemptedAddress}: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles rapid reconnection strategy when connection is lost
    /// </summary>
    private void HandleDisconnectedState()
    {
        if (!isReconnecting)
        {
            Debug.Log("[MotionStreamer] Robot disconnected. Starting rapid reconnection process...");
            isReconnecting = true;
            reconnectTimer = 0f;
            reconnectAttempts = 0;
            
            // More thorough cleanup of the old connection
            if (frcDataSink != null && frcDataSink.Client != null)
            {
                try
                {
                    frcDataSink.Client.Disconnect();
                    // Force garbage collection to clean up socket resources
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MotionStreamer] Error during connection cleanup: {ex.Message}");
                }
                
                // Set to null to ensure complete recreation
                frcDataSink = null;
            }
        }
        
        // Handle reconnection with fixed short delay
        reconnectTimer += Time.deltaTime;
        if (reconnectTimer >= reconnectDelay)
        {
            reconnectAttempts++;
            Debug.Log($"[MotionStreamer] Reconnection attempt {reconnectAttempts}");
            
            // Toggle between connection methods to increase chances of success
            useAddress = !useAddress;
            
            // Create a fresh client instance
            ConnectToRobot();
            
            reconnectTimer = 0f;
        }
    }

    /// <summary>
    /// Publishes and subscribes to required NetworkTables topics
    /// </summary>
    private void PublishTopics()
    {
        try
        {
            if (frcDataSink == null) return;
            
            frcDataSink.PublishTopic("/questnav/miso", "int");
            frcDataSink.PublishTopic("/questnav/frameCount", "int");
            frcDataSink.PublishTopic("/questnav/timestamp", "double");
            frcDataSink.PublishTopic("/questnav/position", "float[]");
            frcDataSink.PublishTopic("/questnav/quaternion", "float[]");
            frcDataSink.PublishTopic("/questnav/eulerAngles", "float[]");
            frcDataSink.PublishTopic("/questnav/batteryPercent", "double");
            frcDataSink.Subscribe("/questnav/mosi", 0.1, false, false, false);
            frcDataSink.Subscribe("/questnav/init/position", 0.1, false, false, false);
            frcDataSink.Subscribe("/questnav/init/eulerAngles", 0.1, false, false, false);
            frcDataSink.Subscribe("/questnav/resetpose", 0.1, false, false, false);
            
            topicsPublished = true;
            Debug.Log("[MotionStreamer] Topics published successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MotionStreamer] Error publishing topics: {ex.Message}");
            topicsPublished = false;
        }
    }
    #endregion

    #region Data Publishing Methods
    /// <summary>
    /// Publishes current frame data to NetworkTables
    /// </summary>
    private void PublishFrameData()
    {
        try 
        {
            // Get current values from camera rig
            frameIndex = Time.frameCount;
            timeStamp = Time.time;
            position = cameraRig.centerEyeAnchor.position;
            rotation = cameraRig.centerEyeAnchor.rotation;
            eulerAngles = cameraRig.centerEyeAnchor.eulerAngles;
            batteryPercent = SystemInfo.batteryLevel * 100;

            // Publish values to NetworkTables
            frcDataSink.PublishValue("/questnav/frameCount", frameIndex);
            frcDataSink.PublishValue("/questnav/timestamp", timeStamp);
            frcDataSink.PublishValue("/questnav/position", position.ToArray());
            frcDataSink.PublishValue("/questnav/quaternion", rotation.ToArray());
            frcDataSink.PublishValue("/questnav/eulerAngles", eulerAngles.ToArray());
            frcDataSink.PublishValue("/questnav/batteryPercent", batteryPercent);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MotionStreamer] Error publishing frame data: {ex.Message}");
            HandleDisconnectedState();
        }
    }
    #endregion

    #region Command Processing Methods
    /// <summary>
    /// Processes commands received from the robot
    /// </summary>
    private void ProcessCommands()
    {
        try
        {
            command = frcDataSink.GetLong("/questnav/mosi");
            lastReceiveTime = Time.time; // Update time when we successfully get data

            if (resetInProgress && command == 0)
            {
                resetInProgress = false;
                Debug.Log("[MotionStreamer] Reset operation completed");
                return;
            }

            switch (command)
            {
                case 1:
                    if (!resetInProgress)
                    {
                        Debug.Log("[MotionStreamer] Received heading reset request, initiating recenter...");
                        RecenterPlayer();
                        resetInProgress = true;
                    }
                    break;
                case 2:
                    if (!resetInProgress)
                    {
                        Debug.Log("[MotionStreamer] Received pose reset request, initiating reset...");
                        InitiatePoseReset();
                        Debug.Log("[MotionStreamer] Processing pose reset request.");
                        resetInProgress = true;
                    }
                    break;
                case 3:
                    Debug.Log("[MotionStreamer] Ping received, responding...");
                    frcDataSink.PublishValue("/questnav/miso", 97);  // 97 for ping response
                    break;
                default:
                    if (!resetInProgress)
                    {
                        frcDataSink.PublishValue("/questnav/miso", 0);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MotionStreamer] Error processing commands: {ex.Message}");
        }
    }

    /// <summary>
    /// Initiates a pose reset based on received NetworkTables data
    /// </summary>
    private void InitiatePoseReset()
    {
        try {
            // Constants for retry logic and field dimensions
            const int maxRetries = 3;              // Maximum number of attempts to read pose data
            const float retryDelayMs = 50f;        // Delay between retry attempts
            const float FIELD_LENGTH = 16.54f;     // FRC field length in meters
            const float FIELD_WIDTH = 8.02f;       // FRC field width in meters

            double[] resetPose = null;
            bool success = false;
            int attemptCount = 0;

            // Attempt to read pose data from NetworkTables with retry logic
            for (int i = 0; i < maxRetries && !success; i++)
            {
                attemptCount++;
                // Add delay between retries to allow for network latency
                if (i > 0) {
                    System.Threading.Thread.Sleep((int)retryDelayMs);
                    Debug.Log($"[MotionStreamer] Attempt {attemptCount} of {maxRetries}...");
                }

                Debug.Log($"[MotionStreamer] Reading NetworkTables Values (Attempt {attemptCount}):");

                // Read the pose array from NetworkTables
                // Format: [X, Y, Rotation] in FRC field coordinates
                resetPose = frcDataSink.GetDoubleArray("/questnav/resetpose");

                // Validate pose data format and field boundaries
                if (resetPose != null && resetPose.Length == 3) {
                    // Check if pose is within valid field boundaries
                    if (resetPose[0] < 0 || resetPose[0] > FIELD_LENGTH ||
                        resetPose[1] < 0 || resetPose[1] > FIELD_WIDTH) {
                        Debug.LogWarning($"[MotionStreamer] Reset pose outside field boundaries: X:{resetPose[0]:F3} Y:{resetPose[1]:F3}");
                        continue;
                    }
                    success = true;
                    Debug.Log($"[MotionStreamer] Successfully read reset pose values on attempt {attemptCount}");
                }

                Debug.Log($"[MotionStreamer] Values (Attempt {attemptCount}): X:{resetPose?[0]:F3} Y:{resetPose?[1]:F3} Rot:{resetPose?[2]:F3}");
            }

            // Exit if we couldn't get valid pose data
            if (!success) {
                Debug.LogWarning($"[MotionStreamer] Failed to read valid reset pose values after {attemptCount} attempts");
                frcDataSink.PublishValue("/questnav/miso", 0);
                resetInProgress = false;
                return;
            }

            // Extract pose components from the array
            double resetX = resetPose[0];          // FRC X coordinate (along length of field)
            double resetY = resetPose[1];          // FRC Y coordinate (along width of field)
            double resetRotation = resetPose[2];   // FRC rotation (CCW positive)

            // Normalize rotation to -180 to 180 degrees range
            while (resetRotation > 180) resetRotation -= 360;
            while (resetRotation < -180) resetRotation += 360;

            Debug.Log($"[MotionStreamer] Starting pose reset - Target: FRC X:{resetX:F2} Y:{resetY:F2} Rot:{resetRotation:F2}°");

            // Store current VR camera state for reference
            Vector3 currentCameraPos = vrCamera.position;
            Quaternion currentCameraRot = vrCamera.rotation;

            Debug.Log($"[MotionStreamer] Before reset - Camera Pos:{currentCameraPos:F3} Rot:{currentCameraRot.eulerAngles:F3}");

            // Convert FRC coordinates to Unity coordinates:
            // - Unity X = -FRC Y (right is positive in Unity)
            // - Unity Y = kept unchanged (height)
            // - Unity Z = FRC X (forward is positive in both)
            Vector3 targetUnityPosition = new Vector3(
                (float)(-resetY),         // Negate Y for Unity's coordinate system
                currentCameraPos.y,       // Maintain current height
                (float)resetX            // FRC X maps directly to Unity Z
            );

            // Calculate the position difference we need to move
            Vector3 positionDelta = targetUnityPosition - currentCameraPos;

            // Convert FRC rotation to Unity rotation:
            // - Unity uses clockwise positive rotation around Y
            // - FRC uses counterclockwise positive rotation
            float targetUnityYaw = (float)(-resetRotation);  // Negate for Unity's rotation direction
            float currentYaw = currentCameraRot.eulerAngles.y;

            // Normalize both angles to 0-360 range for proper delta calculation
            while (currentYaw < 0) currentYaw += 360;
            while (targetUnityYaw < 0) targetUnityYaw += 360;

            // Calculate rotation delta and normalize to -180 to 180 range
            float yawDelta = targetUnityYaw - currentYaw;
            if (yawDelta > 180) yawDelta -= 360;
            if (yawDelta < -180) yawDelta += 360;

            Debug.Log($"[MotionStreamer] Calculated adjustments - Position delta:{positionDelta:F3} Rotation delta:{yawDelta:F3}°");
            Debug.Log($"[MotionStreamer] Target Unity Position: {targetUnityPosition:F3}");

            // Store the original offset between camera and its root
            // This helps maintain proper VR tracking space
            Vector3 cameraOffset = vrCamera.position - vrCameraRoot.position;

            // First rotate the root around the camera position
            // This maintains the camera's position while changing orientation
            vrCameraRoot.RotateAround(vrCamera.position, Vector3.up, yawDelta);

            // Then apply the position adjustment to achieve target position
            vrCameraRoot.position += positionDelta;

            // Log final position and rotation for verification
            Debug.Log($"[MotionStreamer] After reset - Camera Pos:{vrCamera.position:F3} Rot:{vrCamera.rotation.eulerAngles:F3}");

            // Calculate and check position error to ensure accuracy
            float posError = Vector3.Distance(vrCamera.position, targetUnityPosition);
            Debug.Log($"[MotionStreamer] Position error after reset: {posError:F3}m");

            // Warn if position error is larger than expected threshold
            if (posError > 0.01f) {  // 1cm threshold
                Debug.LogWarning($"[MotionStreamer] Large position error detected!");
            }

            frcDataSink.PublishValue("/questnav/miso", 98);
        }
        catch (Exception e) {
            Debug.LogError($"[MotionStreamer] Error during pose reset: {e.Message}");
            Debug.LogException(e);
            frcDataSink.PublishValue("/questnav/miso", 0);
            resetInProgress = false;
        }
    }

    /// <summary>
    /// Recenters the player's view by adjusting the VR camera root transform to match the reset transform.
    /// Similar to the effect of long-pressing the Oculus button.
    /// </summary>
    void RecenterPlayer()
    {
        try {
            // Calculate the rotation angle difference between current camera and reset position
            float rotationAngleY = vrCamera.rotation.eulerAngles.y - resetTransform.rotation.eulerAngles.y;

            // Apply negative rotation to camera root to align with reset orientation
            vrCameraRoot.transform.Rotate(0, -rotationAngleY, 0);

            // Calculate position difference and apply to camera root
            Vector3 distanceDiff = resetTransform.position - vrCamera.position;
            vrCameraRoot.transform.position += distanceDiff;

            // Send success response to robot
            frcDataSink.PublishValue("/questnav/miso", 99);
        }
        catch (Exception e) {
            Debug.LogError($"[MotionStreamer] Error during player recenter: {e.Message}");
            Debug.LogException(e);
            frcDataSink.PublishValue("/questnav/miso", 0);
            resetInProgress = false;
        }
    }
    #endregion

    #region UI Methods
    /// <summary>
    /// Updates the team number based on user input and triggers a connection reset
    /// </summary>
    public void UpdateTeamNumber()
    {
        Debug.Log("[MotionStreamer] Updating Team Number");
        teamNumber = teamInput.text;
        PlayerPrefs.SetString("TeamNumber", teamNumber);
        PlayerPrefs.Save();
        setInputBox(teamNumber);
        HandleDisconnectedState();
    }

    /// <summary>
    /// Sets the input box placeholder text with the current team number
    /// </summary>
    /// <param name="team">The team number to display</param>
    private void setInputBox(string team)
    {
        teamInput.text = "";
        TextMeshProUGUI placeholderText = teamInput.placeholder as TextMeshProUGUI;
        if (placeholderText != null)
        {
            placeholderText.text = "Current: " + team;
        }
        else
        {
            Debug.LogError("Placeholder is not assigned or not a TextMeshProUGUI component.");
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Generates the DNS address for the roboRIO based on team number
    /// </summary>
    /// <returns>The formatted DNS address string</returns>
    private string getDNS()
    {
        return serverDNS.Replace("####", teamNumber);
    }

    /// <summary>
    /// Generates the IP address for the roboRIO based on team number
    /// </summary>
    /// <returns>The formatted IP address string</returns>
    private string getIP()
    {
        string tePart = teamNumber.Length > 2 ? teamNumber.Substring(0, teamNumber.Length - 2) : "0";
        string amPart = teamNumber.Length > 2 ? teamNumber.Substring(teamNumber.Length - 2) : teamNumber;
        return serverAddress.Replace("TE", tePart).Replace("AM", amPart);
    }

    /// <summary>
    /// Event handler for when the input field is selected
    /// </summary>
    /// <param name="text">The current text in the input field</param>
    private void OnInputFieldSelected(string text)
    {
        Debug.Log("[MotionStreamer] Input Selected");
    }
    #endregion
}